using Mullai.Abstractions.Observability;

namespace Mullai.TUI.TUI.State;

/// <summary>A single message in the conversation.</summary>
public class ChatMessage
{
    public string Content { get; set; }
    public bool IsUser { get; set; }
    public DateTimeOffset Timestamp { get; set; }

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

    public event Action? StateChanged;

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public IReadOnlyList<ToolCallObservation> ToolCalls => _toolCalls;

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
    public string StreamingBuffer { get; private set; } = string.Empty;

    // ── Chat messages ──────────────────────────────────────────────────────────

    public void AddUserMessage(string text)
    {
        _messages.Add(new ChatMessage(text, isUser: true, timestamp: DateTimeOffset.Now));
        Notify();
    }

    public void BeginAgentResponse()
    {
        IsThinking = true;
        StreamingBuffer = string.Empty;
        Notify();
    }

    public void AppendUpdate(string token, bool firstUpdate) 
    {
        StreamingBuffer += token;

        if (firstUpdate)
        {
            _messages.Add(new ChatMessage(StreamingBuffer, isUser: false, timestamp: DateTimeOffset.Now));   
        }
        else
        {
            _messages[^1].Content =  StreamingBuffer;
        }
        
        IsThinking = false;
        
        Notify();
    }

    public void CompleteAgentResponse()
    {
        StreamingBuffer = string.Empty;
        IsThinking = false;
        Notify();
    }

    public void AddErrorMessage(string error)
    {
        IsThinking = false;
        StreamingBuffer = string.Empty;
        _messages.Add(new ChatMessage($"⚠ {error}", isUser: false, timestamp: DateTimeOffset.Now));
        Notify();
    }

    // ── Tool calls ──────────────────────────────────────────────────────────────

    /// <summary>Append a completed tool call observation. Called from ChatController's pump loop.</summary>
    public void AddToolCall(ToolCallObservation observation)
    {
        _toolCalls.Add(observation);
        Notify();
    }

    public void Notify() => StateChanged?.Invoke();
}
