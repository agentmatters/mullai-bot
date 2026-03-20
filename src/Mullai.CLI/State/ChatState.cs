using Mullai.Abstractions.Observability;
using Mullai.Abstractions.Orchestration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mullai.CLI.State;

/// <summary>A single message in the conversation.</summary>
public class ChatMessage
{
    public string Content { get; set; }
    public bool IsUser { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public ChatMessage(string content, bool isUser,  DateTimeOffset timestamp)
    {
        Content = content;
        Timestamp = timestamp;
        IsUser = isUser;
    }
}

/// <summary>
/// Shared, observable state for the chat session.
/// Views subscribe to <see cref="StateChanged"/> and pull data on notification.
/// </summary>
public class ChatState
{
    private readonly List<ChatMessage> _messages = [];
    private readonly List<ToolCallObservation> _toolCalls = [];
    private TaskGraph _currentGraph = new();

    public event Action? StateChanged;

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public IReadOnlyList<ToolCallObservation> ToolCalls => _toolCalls;
    public TaskGraph CurrentGraph => _currentGraph;

    public int CompletedTasks => _currentGraph.Nodes.Count(t => t.Status == Mullai.Abstractions.Orchestration.TaskStatus.Completed);
    public double Progress => _currentGraph.Nodes.Count == 0 ? 0 : (double)CompletedTasks / _currentGraph.Nodes.Count;

    public IEnumerable<TaskNode> GetRootTasks() => 
        _currentGraph.Nodes.Where(n => !_currentGraph.Edges.Any(e => e.ToId == n.Id));

    public IEnumerable<TaskNode> GetChildren(string taskId) => 
        _currentGraph.Edges.Where(e => e.FromId == taskId)
            .Select(e => _currentGraph.Nodes.FirstOrDefault(n => n.Id == e.ToId))
            .Where(n => n != null)!;

    /// <summary>A unified sequence of messages and tool calls sorted by time.</summary>
    public IEnumerable<object> ChronologicalEntries =>
        _messages.Cast<object>()
            .Concat(_toolCalls.Cast<object>())
            .OrderBy(e => e switch
            {
                ChatMessage m => m.Timestamp,
                ToolCallObservation t => t.StartedAt,
                _ => DateTimeOffset.MinValue
            });

    public bool IsThinking { get; private set; }
    private readonly ConcurrentDictionary<string, string> _streamingBuffers = new();

    // ── Chat messages ──────────────────────────────────────────────────────────

    public void AddUserMessage(string text)
    {
        _messages.Add(new ChatMessage(text, isUser: true, timestamp: DateTimeOffset.Now));
        Notify();
    }

    public void BeginAgentResponse()
    {
        IsThinking = true;
        _streamingBuffers.Clear();
        Notify();
    }

    public void AppendUpdate(string taskId, string token, string agentName) 
    {
        var sourceId = string.IsNullOrEmpty(agentName) ? taskId : agentName;
        var buffer = _streamingBuffers.AddOrUpdate(taskId, token, (id, old) => old + token);
        
        // Find the LATEST message for this TaskId that is NOT a user message
        var msg = _messages.LastOrDefault(m => !m.IsUser && m.Metadata.ContainsKey("TaskId") && m.Metadata["TaskId"].ToString() == taskId);

        // If no message exists OR the buffer was recently flushed (meaning we want a new block)
        if (msg == null || string.IsNullOrEmpty(buffer))
        {
            // If the buffer was empty (flushed), use the first token as the start
            if (string.IsNullOrEmpty(buffer)) buffer = token;
            
            msg = new ChatMessage(buffer, isUser: false, timestamp: DateTimeOffset.Now);
            msg.Metadata["TaskId"] = taskId;
            msg.Metadata["SourceId"] = sourceId;
            _messages.Add(msg);
        }
        else
        {
            msg.Content = buffer;
            msg.Metadata["SourceId"] = sourceId;
        }
    
        IsThinking = false;
        Notify();
    }

    public void FlushBuffer(string taskId)
    {
        _streamingBuffers.TryRemove(taskId, out _);
    }

    public void CompleteAgentResponse()
    {
        _streamingBuffers.Clear();
        IsThinking = false;
        Notify();
    }

    public void AddErrorMessage(string error)
    {
        IsThinking = false;
        _messages.Add(new ChatMessage($"⚠ {error}", isUser: false, timestamp: DateTimeOffset.Now));
        Notify();
    }

    public void SetGraph(TaskGraph graph)
    {
        _currentGraph = graph;
        Notify();
    }

    public void UpdateTaskStatus(string taskId, string status)
    {
        var node = _currentGraph.Nodes.FirstOrDefault(n => n.Id == taskId);
        if (node != null)
        {
            if (Enum.TryParse<Mullai.Abstractions.Orchestration.TaskStatus>(status, true, out var taskStatus))
            {
                node.Status = taskStatus;
                Notify();
            }
        }
    }

    // ── Tool calls ──────────────────────────────────────────────────────────────

    /// <summary>Append a completed tool call observation. Called from ChatController's pump loop.</summary>
    public void AddToolCall(ToolCallObservation observation)
    {
        // Flush all buffers to ensure strict chronological flow between text blocks and tool calls.
        // This prevents text from jumping over tool observations in the UI.
        _streamingBuffers.Clear();
        
        _toolCalls.Add(observation);
        Notify();
    }

    public void Notify() => StateChanged?.Invoke();
}
