namespace Mullai.Abstractions.WorkflowState;

public sealed record WorkflowStateRecord
{
    public string WorkflowId { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string JsonValue { get; init; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
