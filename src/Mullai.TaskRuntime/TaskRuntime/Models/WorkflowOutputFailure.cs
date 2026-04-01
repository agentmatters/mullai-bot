namespace Mullai.TaskRuntime.Models;

public sealed record WorkflowOutputFailure
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string WorkflowId { get; init; } = string.Empty;
    public string OutputType { get; init; } = string.Empty;
    public string? OutputTarget { get; init; }
    public IReadOnlyDictionary<string, string> OutputProperties { get; init; } = new Dictionary<string, string>();
    public string TaskId { get; init; } = string.Empty;
    public string SessionKey { get; init; } = string.Empty;
    public string Response { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public string Error { get; init; } = string.Empty;
    public int Attempts { get; init; }
    public DateTimeOffset FailedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}