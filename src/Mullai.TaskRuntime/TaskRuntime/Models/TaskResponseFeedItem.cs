namespace Mullai.TaskRuntime.Models;

public sealed record TaskResponseFeedItem
{
    public string TaskId { get; init; } = string.Empty;
    public string? SessionKey { get; init; }
    public string Response { get; init; } = string.Empty;
    public long InputTokenCount { get; init; }
    public long OutputTokenCount { get; init; }
    public long TotalTokenCount { get; init; }
    public long CumulativeInputTokenCount { get; init; }
    public long CumulativeOutputTokenCount { get; init; }
    public long CumulativeTotalTokenCount { get; init; }
}
