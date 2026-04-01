using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.WorkflowOutputHandlers;

public sealed class LogWorkflowOutputHandler : IWorkflowOutputHandler
{
    private readonly ILogger<LogWorkflowOutputHandler> _logger;

    public LogWorkflowOutputHandler(ILogger<LogWorkflowOutputHandler> logger)
    {
        _logger = logger;
    }

    public string Type => "log";

    public Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Workflow {WorkflowId} output (log): {Response}",
            context.Definition.Id,
            context.Response);
        return Task.CompletedTask;
    }
}