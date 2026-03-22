using Microsoft.Extensions.AI;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowToolsProvider
{
    IReadOnlyList<AITool> GetTools();
}
