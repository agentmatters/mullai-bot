using Mullai.Workflows.Models;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowRegistry
{
    IReadOnlyList<WorkflowDefinition> GetAll();
    WorkflowDefinition? GetById(string workflowId);
}