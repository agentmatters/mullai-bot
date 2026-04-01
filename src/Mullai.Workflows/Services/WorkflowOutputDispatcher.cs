using Microsoft.Extensions.Logging;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.Workflows.Services;

public sealed class WorkflowOutputDispatcher : IWorkflowOutputDispatcher
{
    private readonly IReadOnlyDictionary<string, IWorkflowOutputHandler> _handlers;
    private readonly ILogger<WorkflowOutputDispatcher> _logger;

    public WorkflowOutputDispatcher(IEnumerable<IWorkflowOutputHandler> handlers,
        ILogger<WorkflowOutputDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(handler => handler.Type, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task DispatchAsync(WorkflowOutputContext context, CancellationToken cancellationToken)
    {
        if (context.Definition.Outputs.Count == 0) return;

        foreach (var output in context.Definition.Outputs)
        {
            if (!output.Enabled || string.IsNullOrWhiteSpace(output.Type)) continue;

            if (!_handlers.TryGetValue(output.Type.Trim(), out var handler))
            {
                _logger.LogWarning("No workflow output handler registered for type {OutputType}.", output.Type);
                continue;
            }

            await handler.HandleAsync(context, output, cancellationToken).ConfigureAwait(false);
        }
    }
}