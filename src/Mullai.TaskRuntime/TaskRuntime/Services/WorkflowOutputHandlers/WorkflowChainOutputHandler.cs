using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.WorkflowOutputHandlers;

public sealed class WorkflowChainOutputHandler : IWorkflowOutputHandler
{
    private readonly IMullaiTaskQueue _queue;
    private readonly IMullaiTaskStatusStore _statusStore;
    private readonly IWorkflowRegistry _registry;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly ILogger<WorkflowChainOutputHandler> _logger;

    public WorkflowChainOutputHandler(
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IWorkflowRegistry registry,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<WorkflowChainOutputHandler> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => "workflow";

    public async Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output, CancellationToken cancellationToken)
    {
        var targetWorkflowId = output.Target;
        if (string.IsNullOrWhiteSpace(targetWorkflowId))
        {
            _logger.LogWarning("Workflow output 'workflow' missing target workflow id.");
            return;
        }

        if (_registry.GetById(targetWorkflowId) is null)
        {
            _logger.LogWarning("Workflow output target {WorkflowId} not found.", targetWorkflowId);
            return;
        }

        var input = ResolveInput(context, output);
        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.LogWarning("Workflow output 'workflow' has no input for target {WorkflowId}.", targetWorkflowId);
            return;
        }

        var maxAttempts = Math.Max(1, _runtimeOptions.DefaultMaxAttempts);
        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = $"{context.SessionKey}:{targetWorkflowId}",
            AgentName = $"workflow:{targetWorkflowId}",
            Prompt = input,
            Source = MullaiTaskSource.System,
            MaxAttempts = maxAttempts,
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = targetWorkflowId,
                ["triggeredBy"] = context.Definition.Id
            }
        };

        await _queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await _statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveInput(WorkflowOutputContext context, WorkflowOutputDefinition output)
    {
        if (output.Properties.TryGetValue("input", out var template) && !string.IsNullOrWhiteSpace(template))
        {
            return template.Replace("{{response}}", context.Response, StringComparison.OrdinalIgnoreCase);
        }

        return context.Response;
    }
}
