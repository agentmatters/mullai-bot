namespace Mullai.Workflows.Models;

public sealed class WorkflowOutputDefinition
{
    public string Type { get; init; } = string.Empty;
    public string? Target { get; init; }
    public bool Enabled { get; init; } = true;
    public Dictionary<string, string> Properties { get; init; } = [];
}