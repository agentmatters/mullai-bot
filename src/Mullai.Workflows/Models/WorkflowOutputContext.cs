namespace Mullai.Workflows.Models;

public sealed class WorkflowOutputContext
{
    public required WorkflowDefinition Definition { get; init; }
    public required string Response { get; init; }
    public required string TaskId { get; init; }
    public required string SessionKey { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
