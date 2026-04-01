using Cronos;
using Mullai.Abstractions.WorkflowState;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.Background;

public sealed class WorkflowTriggerSchedulerService : BackgroundService
{
    private readonly ILogger<WorkflowTriggerSchedulerService> _logger;
    private readonly IMullaiTaskQueue _queue;
    private readonly IWorkflowRegistry _registry;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly IWorkflowStateStore _stateStore;
    private readonly IMullaiTaskStatusStore _statusStore;

    public WorkflowTriggerSchedulerService(
        IWorkflowRegistry registry,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IWorkflowStateStore stateStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<WorkflowTriggerSchedulerService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedules = new Dictionary<string, TriggerSchedule>(StringComparer.OrdinalIgnoreCase);
        var timeZone = TimeZoneInfo.Local;
        var pollInterval = TimeSpan.FromSeconds(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workflows = _registry.GetAll();
                var now = DateTimeOffset.Now;

                var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var workflow in workflows)
                {
                    if (!workflow.IsEnabled) continue;

                    foreach (var trigger in workflow.Triggers.Where(t => t.Enabled))
                    {
                        var key = $"{workflow.Id}:{trigger.Id}";
                        activeKeys.Add(key);

                        if (!schedules.TryGetValue(key, out var schedule))
                        {
                            schedule = CreateSchedule(workflow, trigger, timeZone);
                            if (schedule is null) continue;

                            schedules[key] = schedule;
                        }

                        if (schedule.NextRunUtc is not null && schedule.NextRunUtc <= now)
                        {
                            if (!await IsStopConditionMetAsync(workflow.Id, trigger, stoppingToken)
                                    .ConfigureAwait(false))
                                await EnqueueWorkflowAsync(workflow, trigger, stoppingToken).ConfigureAwait(false);
                            else
                                _logger.LogInformation(
                                    "Skipping trigger {TriggerId} for workflow {WorkflowId} due to stop condition.",
                                    trigger.Id,
                                    workflow.Id);
                            schedule.NextRunUtc = ComputeNextRun(schedule, now, timeZone);
                        }
                    }
                }

                foreach (var stale in schedules.Keys.Where(k => !activeKeys.Contains(k)).ToList())
                    schedules.Remove(stale);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Workflow trigger scheduler loop failed.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private TriggerSchedule? CreateSchedule(WorkflowDefinition workflow, WorkflowTriggerDefinition trigger,
        TimeZoneInfo timeZone)
    {
        if (trigger.Type.Equals("cron", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(trigger.Cron))
            {
                _logger.LogWarning("Cron trigger on workflow {WorkflowId} is missing cron expression.", workflow.Id);
                return null;
            }

            try
            {
                var format = trigger.Cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 6
                    ? CronFormat.IncludeSeconds
                    : CronFormat.Standard;
                var expression = CronExpression.Parse(trigger.Cron, format);
                var next = expression.GetNextOccurrence(DateTimeOffset.Now, timeZone);
                _logger.LogInformation(
                    "Registered cron trigger {TriggerId} for workflow {WorkflowId} with cron {Cron}.",
                    trigger.Id,
                    workflow.Id,
                    trigger.Cron);
                return new TriggerSchedule(trigger.Type, expression, trigger.IntervalSeconds, next);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid cron expression '{Cron}' for workflow {WorkflowId}.", trigger.Cron,
                    workflow.Id);
                return null;
            }
        }

        if (trigger.Type.Equals("interval", StringComparison.OrdinalIgnoreCase))
        {
            if (trigger.IntervalSeconds is null || trigger.IntervalSeconds <= 0)
            {
                _logger.LogWarning("Interval trigger on workflow {WorkflowId} is missing intervalSeconds.",
                    workflow.Id);
                return null;
            }

            _logger.LogInformation(
                "Registered interval trigger {TriggerId} for workflow {WorkflowId} every {IntervalSeconds}s.",
                trigger.Id,
                workflow.Id,
                trigger.IntervalSeconds);
            return new TriggerSchedule(trigger.Type, null, trigger.IntervalSeconds,
                DateTimeOffset.Now.AddSeconds(trigger.IntervalSeconds.Value));
        }

        _logger.LogInformation(
            "Skipping unsupported workflow trigger type {TriggerType} for workflow {WorkflowId}.",
            trigger.Type,
            workflow.Id);
        return null;
    }

    private static DateTimeOffset? ComputeNextRun(TriggerSchedule schedule, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        if (schedule.Type.Equals("cron", StringComparison.OrdinalIgnoreCase) && schedule.CronExpression is not null)
            return schedule.CronExpression.GetNextOccurrence(now, timeZone);

        if (schedule.Type.Equals("interval", StringComparison.OrdinalIgnoreCase) &&
            schedule.IntervalSeconds is not null) return now.AddSeconds(schedule.IntervalSeconds.Value);

        return null;
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

    private async Task<bool> IsStopConditionMetAsync(
        string workflowId,
        WorkflowTriggerDefinition trigger,
        CancellationToken cancellationToken)
    {
        if (!trigger.Properties.TryGetValue("stopKey", out var key) || string.IsNullOrWhiteSpace(key)) return false;

        var record = await _stateStore.GetAsync(workflowId, key, cancellationToken).ConfigureAwait(false);
        if (record is null) return false;

        if (!trigger.Properties.TryGetValue("stopValue", out var stopValue) || string.IsNullOrWhiteSpace(stopValue))
            return IsTruthy(record.JsonValue);

        return NormalizeJsonValue(record.JsonValue)
            .Equals(stopValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTruthy(string json)
    {
        var normalized = NormalizeJsonValue(json);
        return string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeJsonValue(string json)
    {
        var trimmed = json.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
            return trimmed[1..^1];

        return trimmed;
    }

    private sealed class TriggerSchedule
    {
        public TriggerSchedule(string type, CronExpression? cronExpression, int? intervalSeconds,
            DateTimeOffset? nextRunUtc)
        {
            Type = type;
            CronExpression = cronExpression;
            IntervalSeconds = intervalSeconds;
            NextRunUtc = nextRunUtc;
        }

        public string Type { get; }
        public CronExpression? CronExpression { get; }
        public int? IntervalSeconds { get; }
        public DateTimeOffset? NextRunUtc { get; set; }
    }
}