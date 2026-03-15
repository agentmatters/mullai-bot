using Mullai.Abstractions.Observability;

namespace Mullai.TUI.TUI.State;

/// <summary>A single message in the conversation.</summary>
public record ChatMessage(string Content, bool IsUser, DateTimeOffset Timestamp);

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
        _messages.Add(new ChatMessage(text, IsUser: true, Timestamp: DateTimeOffset.Now));
        Notify();
    }

    public void BeginAgentResponse()
    {
        IsThinking = true;
        StreamingBuffer = string.Empty;
        Notify();
    }

    public void AppendToken(string token)
    {
        StreamingBuffer += token;
        Notify();
    }

    public void CommitAgentResponse()
    {
        if (!string.IsNullOrEmpty(StreamingBuffer))
            _messages.Add(new ChatMessage(StreamingBuffer, IsUser: false, Timestamp: DateTimeOffset.Now));

        StreamingBuffer = string.Empty;
        IsThinking = false;
        Notify();
    }

    public void AddErrorMessage(string error)
    {
        IsThinking = false;
        StreamingBuffer = string.Empty;
        _messages.Add(new ChatMessage($"⚠ {error}", IsUser: false, Timestamp: DateTimeOffset.Now));
        Notify();
    }

    // ── Tool calls ──────────────────────────────────────────────────────────────

    /// <summary>Append a completed tool call observation. Called from ChatController's pump loop.</summary>
    public void AddToolCall(ToolCallObservation observation)
    {
        _toolCalls.Add(observation);
        Notify();
    }

    private void Notify() => StateChanged?.Invoke();
}
