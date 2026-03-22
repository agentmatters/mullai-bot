using Mullai.Abstractions.Observability;

namespace Mullai.TaskRuntime.Models;

public sealed record ToolCallFeedItem
{
    public long Sequence { get; init; }
    public string? TaskId { get; init; }
    public string? SessionKey { get; init; }
    public ToolCallObservation Observation { get; init; } = default!;
}
