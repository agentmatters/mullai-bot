using Microsoft.Extensions.AI;
using Mullai.Abstractions.Orchestration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Mullai.Web.Wasm.Messaging;

namespace Mullai.Web.Wasm.Services;

public class ChatState : IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IConfiguration _configuration;
    private readonly IJSRuntime _jsRuntime;
    
    private HubConnection? _hubConnection;
    private string _sessionId = Guid.NewGuid().ToString();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _streamingBuffers = new();

    public List<ChatMessage> Messages { get; } = new();
    public List<ToolCallSnapshot> ToolCalls { get; } = new();
    public Dictionary<string, TaskNode> Tasks { get; } = new();
    public bool IsThinking { get; private set; }
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public string HubUrlDisplay { get; private set; } = "";

    public event Action? OnChange;

    public ChatState(NavigationManager navigationManager, IConfiguration configuration, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _configuration = configuration;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        var hubUrl = GetHubUrl();
        HubUrlDisplay = hubUrl.ToString();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string, string?>("OnTaskUpdate", (nodeId, status, message) =>
        {
            if (Tasks.TryGetValue(nodeId, out var task))
            {
                if (Enum.TryParse<Mullai.Abstractions.Orchestration.TaskStatus>(status, out var taskStatus))
                {
                    task.Status = taskStatus;
                }
            }

            var lastMsg = Messages.LastOrDefault(m => m.Role == ChatRole.Assistant && m.AdditionalProperties?.GetValueOrDefault("TaskId")?.ToString() == nodeId);
            if (lastMsg != null && !string.IsNullOrWhiteSpace(message))
            {
                var updates = lastMsg.AdditionalProperties?.GetValueOrDefault("TaskUpdates") as List<string> ?? new List<string>();
                updates.Add($"[{status}] {message}");
                lastMsg.AdditionalProperties!["TaskUpdates"] = updates;
            }
            NotifyStateChanged();
        });

        _hubConnection.On<TaskGraph>("OnGraphCreated", (graph) =>
        {
            foreach (var node in graph.Nodes)
            {
                Tasks[node.Id] = node;
            }
            NotifyStateChanged();
        });

        _hubConnection.On<string, string, string>("OnAgentToken", (taskId, agentName, token) =>
        {
            AppendToken(taskId, token, agentName);
            IsThinking = false;
            NotifyStateChanged();
        });

        _hubConnection.On<ToolCallSnapshot>("OnToolCall", toolCall =>
        {
            _streamingBuffers.Clear();
            ToolCalls.Add(toolCall);
            NotifyStateChanged();
        });

        _hubConnection.On<string, string>("OnSystemAlert", (level, message) =>
        {
            var msg = new ChatMessage(ChatRole.System, $"**{level.ToUpperInvariant()}:** {message}");
            msg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            msg.AdditionalProperties["Timestamp"] = DateTimeOffset.Now;
            Messages.Add(msg);
            IsThinking = false;
            NotifyStateChanged();
        });

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync("JoinSession", _sessionId);

            // Fetch History
            var history = await _hubConnection.InvokeAsync<List<ChatMessage>>("GetHistory", _sessionId);
            if (history != null && history.Any())
            {
                Messages.Clear();
                Messages.AddRange(history);
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            var msg = new ChatMessage(ChatRole.System, $"**Connection error:** {ex.Message}\n\n**Hub URL:** {HubUrlDisplay}");
            msg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            msg.AdditionalProperties["Timestamp"] = DateTimeOffset.Now;
            Messages.Add(msg);
            IsThinking = false;
            NotifyStateChanged();
        }
    }

    public async Task SendMessageAsync(string userText, ExecutionMode mode)
    {
        if (string.IsNullOrWhiteSpace(userText) || IsThinking)
            return;

        var msg = new ChatMessage(ChatRole.User, userText);
        msg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        msg.AdditionalProperties["Timestamp"] = DateTimeOffset.Now;
        Messages.Add(msg);
        
        IsThinking = true;
        _streamingBuffers.Clear();
        
        NotifyStateChanged();
        await ScrollToBottom();

        try
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.SendAsync("SendMessage", _sessionId, userText, mode.ToString());
            }
        }
        catch (Exception ex)
        {
            var errorMsg = new ChatMessage(ChatRole.System, $"**Error:** {ex.Message}");
            errorMsg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            errorMsg.AdditionalProperties["Timestamp"] = DateTimeOffset.Now;
            Messages.Add(errorMsg);
            IsThinking = false;
            NotifyStateChanged();
        }
    }

    private void AppendToken(string taskId, string token, string agentName)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return;

        var buffer = _streamingBuffers.AddOrUpdate(taskId, token, (_, existing) => existing + token);

        var msg = Messages.LastOrDefault(m => m.Role == ChatRole.Assistant
            && m.AdditionalProperties?.TryGetValue("TaskId", out var id) == true
            && id?.ToString() == taskId);

        if (msg == null || string.IsNullOrEmpty(buffer))
        {
            msg = new ChatMessage(ChatRole.Assistant, buffer);
            msg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            msg.AdditionalProperties["TaskId"] = taskId;
            msg.AdditionalProperties["SourceId"] = agentName;
            msg.AdditionalProperties["Timestamp"] = DateTimeOffset.Now;
            msg.AdditionalProperties["TaskUpdates"] = new List<string>();
            Messages.Add(msg);
        }
        else
        {
            msg.Contents.Clear();
            msg.Contents.Add(new TextContent(buffer));
            msg.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            msg.AdditionalProperties["SourceId"] = agentName;
        }
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("chat_scroll_bottom");
        }
        catch { }
    }

    private Uri GetHubUrl()
    {
        var configuredBaseUrl = _configuration["ChannelsApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return _navigationManager.ToAbsoluteUri("/hubs/fabric");
        }

        configuredBaseUrl = configuredBaseUrl.TrimEnd('/');
        return new Uri($"{configuredBaseUrl}/hubs/fabric");
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            try
            {
                await _hubConnection.SendAsync("LeaveSession", _sessionId);
            }
            catch { }
            await _hubConnection.DisposeAsync();
        }
    }

    public IEnumerable<object> GetVisibleEntries()
    {
        return Messages.Cast<object>()
            .Concat(ToolCalls.Cast<object>())
            .OrderBy(entry => entry switch
            {
                ChatMessage m => m.AdditionalProperties?.TryGetValue("Timestamp", out var ts) == true && ts is DateTimeOffset dto ? dto : DateTimeOffset.MinValue,
                ToolCallSnapshot t => t.StartedAt,
                _ => DateTimeOffset.MinValue
            });
    }
}
