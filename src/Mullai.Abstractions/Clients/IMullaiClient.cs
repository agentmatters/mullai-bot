using Mullai.Abstractions.Orchestration;
using Mullai.Abstractions.Observability;

namespace Mullai.Abstractions.Clients;

/// <summary>
/// A unified interface for connecting to the Mullai orchestration fabric.
/// Simplifies planning, task execution, and real-time event observation.
/// </summary>
public interface IMullaiClient
{
    /// <summary> Ensures a session is active and prepares internal message pumps. </summary>
    Task InitialiseAsync(string sessionId = "default", CancellationToken ct = default);

    /// <summary> Submits a high-level prompt to the planner and starts execution. </summary>
    Task SendPromptAsync(string input, ExecutionMode mode = ExecutionMode.Team, CancellationToken ct = default);

    /// <summary> Provides a consolidated stream of all tokens, status changes, and tool observations. </summary>
    IAsyncEnumerable<MullaiUpdate> GetUpdatesAsync(CancellationToken ct = default);
}

/// <summary> Represents a single granular update from the orchestration fabric. </summary>
public record MullaiUpdate
{
    public string? TaskId { get; init; }
    public string? AgentName { get; init; }
    public string? Text { get; init; }
    public string? Status { get; init; }
    public TaskGraph? Graph { get; init; }
    public ToolCallObservation? ToolCall { get; init; }
    public UpdateType Type { get; init; }
}

public enum UpdateType
{
    Token,
    Status,
    ToolCall,
    Error,
    Graph
}
