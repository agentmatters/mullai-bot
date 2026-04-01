namespace Mullai.TaskRuntime.Models;

public sealed record MullaiTaskStatusSnapshot
{
    public string TaskId { get; init; } = string.Empty;
    public string SessionKey { get; init; } = string.Empty;
    public string AgentName { get; init; } = "Assistant";
    public MullaiTaskSource Source { get; init; } = MullaiTaskSource.Client;
    public string? WorkflowId { get; init; }
    public MullaiTaskState State { get; init; } = MullaiTaskState.Queued;
    public int Attempt { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public string? Response { get; init; }
    public string? Error { get; init; }
    public long InputTokenCount { get; init; }
    public long OutputTokenCount { get; init; }
    public long TotalTokenCount { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}