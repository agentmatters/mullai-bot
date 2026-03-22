using Cronos;
using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services;

public sealed class WorkflowTriggerSchedulerService : BackgroundService
{
    private readonly IWorkflowRegistry _registry;
    private readonly IMullaiTaskQueue _queue;
    private readonly IMullaiTaskStatusStore _statusStore;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly ILogger<WorkflowTriggerSchedulerService> _logger;

    public WorkflowTriggerSchedulerService(
        IWorkflowRegistry registry,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<WorkflowTriggerSchedulerService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runners = new List<Task>();

        foreach (var workflow in _registry.GetAll())
        {
            foreach (var trigger in workflow.Triggers.Where(t => t.Enabled))
            {
                if (trigger.Type.Equals("cron", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Starting cron trigger {TriggerName} for workflow {WorkflowId} with cron {Cron}.",
                        string.IsNullOrWhiteSpace(trigger.Name) ? trigger.Id : trigger.Name,
                        workflow.Id,
                        trigger.Cron);
                    runners.Add(RunCronTriggerAsync(workflow, trigger, stoppingToken));
                }
                else if (trigger.Type.Equals("interval", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Starting interval trigger {TriggerName} for workflow {WorkflowId} every {IntervalSeconds}s.",
                        string.IsNullOrWhiteSpace(trigger.Name) ? trigger.Id : trigger.Name,
                        workflow.Id,
                        trigger.IntervalSeconds);
                    runners.Add(RunIntervalTriggerAsync(workflow, trigger, stoppingToken));
                }
                else
                {
                    _logger.LogInformation(
                        "Skipping unsupported workflow trigger type {TriggerType} for workflow {WorkflowId}.",
                        trigger.Type,
                        workflow.Id);
                }
            }
        }

        if (runners.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(runners);
    }

    private async Task RunCronTriggerAsync(
        WorkflowDefinition workflow,
        WorkflowTriggerDefinition trigger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trigger.Cron))
        {
            _logger.LogWarning("Cron trigger on workflow {WorkflowId} is missing cron expression.", workflow.Id);
            return;
        }

        CronExpression expression;
        try
        {
            var format = trigger.Cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 6
                ? CronFormat.IncludeSeconds
                : CronFormat.Standard;
            expression = CronExpression.Parse(trigger.Cron, format);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid cron expression '{Cron}' for workflow {WorkflowId}.", trigger.Cron, workflow.Id);
            return;
        }

        var timeZone = TimeZoneInfo.Local;

        while (!cancellationToken.IsCancellationRequested)
        {
            var nextUtc = expression.GetNextOccurrence(DateTimeOffset.Now, timeZone);
            if (nextUtc is null)
            {
                return;
            }

            _logger.LogInformation(
                "Cron trigger {TriggerId} for workflow {WorkflowId} scheduled at {NextRun}.",
                trigger.Id,
                workflow.Id,
                nextUtc.Value);

            var delay = nextUtc.Value - DateTimeOffset.Now;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            await EnqueueWorkflowAsync(workflow, trigger, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunIntervalTriggerAsync(
        WorkflowDefinition workflow,
        WorkflowTriggerDefinition trigger,
        CancellationToken cancellationToken)
    {
        if (trigger.IntervalSeconds is null || trigger.IntervalSeconds <= 0)
        {
            _logger.LogWarning("Interval trigger on workflow {WorkflowId} is missing intervalSeconds.", workflow.Id);
            return;
        }

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(trigger.IntervalSeconds.Value));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await EnqueueWorkflowAsync(workflow, trigger, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task EnqueueWorkflowAsync(
        WorkflowDefinition workflow,
        WorkflowTriggerDefinition trigger,
        CancellationToken cancellationToken)
    {
        var input = trigger.Input;
        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.LogWarning("Trigger {TriggerId} for workflow {WorkflowId} has no input.", trigger.Id, workflow.Id);
            return;
        }

        var sessionKey = string.IsNullOrWhiteSpace(trigger.SessionKey)
            ? $"workflow-{workflow.Id}-{trigger.Id}"
            : trigger.SessionKey.Trim();

        var maxAttempts = Math.Max(1, _runtimeOptions.DefaultMaxAttempts);
        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = sessionKey,
            AgentName = $"workflow:{workflow.Id}",
            Prompt = input.Trim(),
            Source = MullaiTaskSource.System,
            MaxAttempts = maxAttempts,
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = workflow.Id,
                ["triggerId"] = trigger.Id,
                ["triggerType"] = trigger.Type
            }
        };

        await _queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await _statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Enqueued workflow {WorkflowId} from trigger {TriggerId} ({TriggerType}).",
            workflow.Id,
            trigger.Id,
            trigger.Type);
    }
}
