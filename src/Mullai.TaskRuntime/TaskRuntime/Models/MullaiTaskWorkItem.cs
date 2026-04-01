namespace Mullai.TaskRuntime.Models;

public sealed record MullaiTaskWorkItem
{
    public string TaskId { get; init; } = Guid.NewGuid().ToString("N");
    public string SessionKey { get; init; } = "default";
    public string AgentName { get; init; } = "Assistant";
    public string Prompt { get; init; } = string.Empty;
    public MullaiTaskSource Source { get; init; } = MullaiTaskSource.Client;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public int Attempt { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}