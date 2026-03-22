using Mullai.Workflows.Models;

namespace Mullai.Workflows.Options;

public sealed class WorkflowRegistryOptions
{
    public const string SectionName = "Mullai:Workflows";

    public List<WorkflowDefinition> Definitions { get; init; } = [];
}
