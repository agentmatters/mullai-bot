using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Execution;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;

namespace Mullai.TaskRuntime.Services;

public class MullaiTaskWorkerService : BackgroundService
{
    private readonly IMullaiTaskQueue _queue;
    private readonly IMullaiTaskStatusStore _statusStore;
    private readonly IMullaiTaskExecutor _executor;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly ILogger<MullaiTaskWorkerService> _logger;

    public MullaiTaskWorkerService(
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IMullaiTaskExecutor executor,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<MullaiTaskWorkerService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _runtimeOptions = runtimeOptions?.Value ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = Math.Max(1, _runtimeOptions.WorkerCount);
        _logger.LogInformation("Starting Mullai task worker service with {WorkerCount} workers", workerCount);

        var workers = Enumerable
            .Range(1, workerCount)
            .Select(workerId => RunWorkerLoopAsync(workerId, stoppingToken))
            .ToArray();

        return Task.WhenAll(workers);
    }

    private async Task RunWorkerLoopAsync(int workerId, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            MullaiTaskWorkItem workItem;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessWorkItemAsync(workerId, workItem, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessWorkItemAsync(int workerId, MullaiTaskWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Worker {WorkerId} processing task {TaskId} (Attempt {Attempt}/{MaxAttempts})",
                workerId,
                workItem.TaskId,
                workItem.Attempt + 1,
                workItem.MaxAttempts);

            await _statusStore.MarkRunningAsync(workItem, cancellationToken).ConfigureAwait(false);
            using var scope = MullaiTaskExecutionContext.BeginScope(workItem.TaskId, workItem.SessionKey);
            var response = await _executor.ExecuteAsync(workItem, cancellationToken).ConfigureAwait(false);
            await _statusStore.MarkSucceededAsync(workItem, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            var nextAttempt = workItem.Attempt + 1;
            var canRetry = nextAttempt < workItem.MaxAttempts;

            _logger.LogError(
                ex,
                "Task {TaskId} failed on attempt {Attempt}/{MaxAttempts}. Retry: {CanRetry}",
                workItem.TaskId,
                workItem.Attempt + 1,
                workItem.MaxAttempts,
                canRetry);

            if (!canRetry)
            {
                await _statusStore.MarkFailedAsync(workItem, ex.Message, cancellationToken).ConfigureAwait(false);
                return;
            }

            var retryItem = workItem with { Attempt = nextAttempt };
            await _statusStore.MarkRetryScheduledAsync(retryItem, ex.Message, cancellationToken).ConfigureAwait(false);

            var delaySeconds = Math.Max(1, _runtimeOptions.RetryDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);

            await _queue.EnqueueAsync(retryItem, cancellationToken).ConfigureAwait(false);
            await _statusStore.MarkQueuedAsync(retryItem, cancellationToken).ConfigureAwait(false);
        }
    }
}
