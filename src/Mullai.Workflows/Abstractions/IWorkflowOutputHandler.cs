using Mullai.Workflows.Models;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowOutputHandler
{
    string Type { get; }

    Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output,
        CancellationToken cancellationToken);
}