using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Mullai.Web.Wasm.Messaging;
using Mullai.Abstractions.Orchestration;

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
    public Dictionary<string, string> TaskStatus { get; } = new();
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
            TaskStatus[nodeId] = status;
            var lastMsg = Messages.LastOrDefault(m => !m.IsUser && m.Metadata.GetValueOrDefault("TaskId")?.ToString() == nodeId);
            if (lastMsg != null && !string.IsNullOrWhiteSpace(message))
            {
                lastMsg.TaskUpdates.Add($"[{status}] {message}");
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
            Messages.Add(new ChatMessage
            {
                Content = $"**{level.ToUpperInvariant()}:** {message}",
                IsUser = false,
                Timestamp = DateTimeOffset.Now
            });
            IsThinking = false;
            NotifyStateChanged();
        });

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync("JoinSession", _sessionId);
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Content = $"**Connection error:** {ex.Message}\n\n**Hub URL:** {HubUrlDisplay}",
                IsUser = false,
                Timestamp = DateTimeOffset.Now
            });
            IsThinking = false;
            NotifyStateChanged();
        }
    }

    public async Task SendMessageAsync(string userText, ExecutionMode mode)
    {
        if (string.IsNullOrWhiteSpace(userText) || IsThinking)
            return;

        Messages.Add(new ChatMessage { Content = userText, IsUser = true, Timestamp = DateTimeOffset.Now });
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
            Messages.Add(new ChatMessage { Content = $"**Error:** {ex.Message}", IsUser = false, Timestamp = DateTimeOffset.Now });
            IsThinking = false;
            NotifyStateChanged();
        }
    }

    private void AppendToken(string taskId, string token, string agentName)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return;

        var buffer = _streamingBuffers.AddOrUpdate(taskId, token, (_, existing) => existing + token);

        var msg = Messages.LastOrDefault(m => !m.IsUser
            && m.Metadata.TryGetValue("TaskId", out var id)
            && id?.ToString() == taskId);

        if (msg == null || string.IsNullOrEmpty(buffer))
        {
            msg = new ChatMessage
            {
                Content = buffer,
                IsUser = false,
                Timestamp = DateTimeOffset.Now
            };
            msg.Metadata["TaskId"] = taskId;
            msg.Metadata["SourceId"] = agentName;
            Messages.Add(msg);
        }
        else
        {
            msg.Content = buffer;
            msg.Metadata["SourceId"] = agentName;
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
                ChatMessage m => m.Timestamp,
                ToolCallSnapshot t => t.StartedAt,
                _ => DateTimeOffset.MinValue
            });
    }
}
