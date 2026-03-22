namespace Mullai.TaskRuntime.Models;

public sealed class MullaiTaskSubmitRequest
{
    public string SessionKey { get; init; } = string.Empty;
    public string AgentName { get; init; } = "Assistant";
    public string Prompt { get; init; } = string.Empty;
    public int? MaxAttempts { get; init; }
    public MullaiTaskSource Source { get; init; } = MullaiTaskSource.Client;
    public Dictionary<string, string>? Metadata { get; init; }
}
