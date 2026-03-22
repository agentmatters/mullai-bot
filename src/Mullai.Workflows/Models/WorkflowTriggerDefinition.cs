namespace Mullai.Workflows.Models;

public sealed class WorkflowTriggerDefinition
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public string? Cron { get; init; }
    public int? IntervalSeconds { get; init; }
    public string? Input { get; init; }
    public string? SessionKey { get; init; }
    public Dictionary<string, string> Properties { get; init; } = [];
}
