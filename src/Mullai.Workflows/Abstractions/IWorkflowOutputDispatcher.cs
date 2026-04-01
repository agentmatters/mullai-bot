using Mullai.Workflows.Models;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowOutputDispatcher
{
    Task DispatchAsync(WorkflowOutputContext context, CancellationToken cancellationToken);
}