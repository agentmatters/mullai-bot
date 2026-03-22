namespace Mullai.TaskRuntime.Models;

public sealed record WorkflowRunEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string TaskId { get; init; } = string.Empty;
    public string? WorkflowId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
