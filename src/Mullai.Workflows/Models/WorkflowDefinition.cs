namespace Mullai.Workflows.Models;

public enum WorkflowKind
{
    SingleAgent = 0,
    ParallelAgents = 1
}

public sealed class WorkflowDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public WorkflowKind Kind { get; init; } = WorkflowKind.SingleAgent;
    public List<WorkflowAgentDefinition> Agents { get; init; } = [];
    public List<WorkflowTriggerDefinition> Triggers { get; init; } = [];
    public List<WorkflowOutputDefinition> Outputs { get; init; } = [];
}

public sealed class WorkflowAgentDefinition
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
}
