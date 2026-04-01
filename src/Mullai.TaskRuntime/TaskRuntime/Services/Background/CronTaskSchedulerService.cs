using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;

namespace Mullai.TaskRuntime.Services.Background;

public class CronTaskSchedulerService : BackgroundService
{
    private readonly ILogger<CronTaskSchedulerService> _logger;
    private readonly IMullaiTaskQueue _queue;
    private readonly MullaiRecurringTaskOptions _recurringOptions;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly IMullaiTaskStatusStore _statusStore;

    public CronTaskSchedulerService(
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiRecurringTaskOptions> recurringOptions,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<CronTaskSchedulerService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _recurringOptions = recurringOptions?.Value ?? throw new ArgumentNullException(nameof(recurringOptions));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jobs = _recurringOptions.Jobs
            .Where(job => job.Enabled && !string.IsNullOrWhiteSpace(job.Prompt) && job.IntervalSeconds > 0)
            .ToArray();

        if (jobs.Length == 0)
        {
            _logger.LogInformation("No recurring Mullai jobs configured.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting recurring task scheduler with {JobCount} jobs", jobs.Length);
        var loops = jobs.Select(job => RunJobLoopAsync(job, stoppingToken)).ToArray();

        return Task.WhenAll(loops);
    }

    private async Task RunJobLoopAsync(MullaiRecurringTaskDefinition job, CancellationToken cancellationToken)
    {
        if (job.RunOnStartup) await EnqueueJobRunAsync(job, cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, job.IntervalSeconds)));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            await EnqueueJobRunAsync(job, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnqueueJobRunAsync(MullaiRecurringTaskDefinition job, CancellationToken cancellationToken)
    {
        var maxAttempts = job.MaxAttempts is > 0 ? job.MaxAttempts.Value : _runtimeOptions.DefaultMaxAttempts;
        var sessionKey = string.IsNullOrWhiteSpace(job.SessionKey) ? $"cron:{job.Name}" : job.SessionKey.Trim();

        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = sessionKey,
            AgentName = string.IsNullOrWhiteSpace(job.AgentName) ? "Assistant" : job.AgentName.Trim(),
            Prompt = job.Prompt.Trim(),
            Source = MullaiTaskSource.Cron,
            MaxAttempts = maxAttempts,
            Metadata = new Dictionary<string, string> { ["JobName"] = job.Name }
        };

        await _queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await _statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Queued recurring task {TaskId} from job {JobName}", workItem.TaskId, job.Name);
    }
}