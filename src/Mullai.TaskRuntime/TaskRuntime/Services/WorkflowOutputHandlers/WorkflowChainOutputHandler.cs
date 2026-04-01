using Mullai.Abstractions.WorkflowState;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.WorkflowOutputHandlers;

public sealed class WorkflowChainOutputHandler : IWorkflowOutputHandler
{
    private readonly ILogger<WorkflowChainOutputHandler> _logger;
    private readonly IMullaiTaskQueue _queue;
    private readonly IWorkflowRegistry _registry;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly IWorkflowStateStore _stateStore;
    private readonly IMullaiTaskStatusStore _statusStore;

    public WorkflowChainOutputHandler(
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IWorkflowStateStore stateStore,
        IWorkflowRegistry registry,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<WorkflowChainOutputHandler> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => "workflow";

    public async Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output,
        CancellationToken cancellationToken)
    {
        if (await IsStopConditionMetAsync(context, output, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation(
                "Workflow chain output stopped for {WorkflowId} due to stop condition.",
                context.Definition.Id);
            return;
        }

        if (!IsCompletionConditionMet(context, output))
        {
            _logger.LogInformation(
                "Workflow chain output skipped for {WorkflowId} because completion token not found.",
                context.Definition.Id);
            return;
        }

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

    private static bool IsCompletionConditionMet(WorkflowOutputContext context, WorkflowOutputDefinition output)
    {
        if (!output.Properties.TryGetValue("completionToken", out var token) ||
            string.IsNullOrWhiteSpace(token))
            return true;

        if (string.IsNullOrWhiteSpace(context.Response)) return false;

        return context.Response.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsStopConditionMetAsync(
        WorkflowOutputContext context,
        WorkflowOutputDefinition output,
        CancellationToken cancellationToken)
    {
        if (!output.Properties.TryGetValue("stopKey", out var stopKey) || string.IsNullOrWhiteSpace(stopKey))
            return false;

        var record = await _stateStore.GetAsync(context.Definition.Id, stopKey, cancellationToken)
            .ConfigureAwait(false);
        if (record is null) return false;

        if (!output.Properties.TryGetValue("stopValue", out var stopValue) ||
            string.IsNullOrWhiteSpace(stopValue)) return true;

        return MatchesStopValue(record.JsonValue, stopValue);
    }

    private static bool MatchesStopValue(string jsonValue, string stopValue)
    {
        if (string.IsNullOrWhiteSpace(jsonValue)) return false;

        var normalizedJson = NormalizeValue(jsonValue);
        var normalizedStop = NormalizeValue(stopValue);

        return string.Equals(normalizedJson, normalizedStop, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal) &&
            trimmed.EndsWith("\"", StringComparison.Ordinal) &&
            trimmed.Length >= 2)
            trimmed = trimmed[1..^1];

        return trimmed.Trim();
    }

    private static string ResolveInput(WorkflowOutputContext context, WorkflowOutputDefinition output)
    {
        if (output.Properties.TryGetValue("input", out var template) && !string.IsNullOrWhiteSpace(template))
            return template.Replace("{{response}}", context.Response, StringComparison.OrdinalIgnoreCase);

        return context.Response;
    }
}