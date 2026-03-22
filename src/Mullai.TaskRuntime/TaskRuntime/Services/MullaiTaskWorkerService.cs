using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Execution;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services;

public class MullaiTaskWorkerService : BackgroundService
{
    private readonly IMullaiTaskQueue _queue;
    private readonly IMullaiTaskStatusStore _statusStore;
    private readonly IMullaiTaskExecutor _executor;
    private readonly IMullaiTaskResponseChannel _responseChannel;
    private readonly IWorkflowRunEventStore _runEventStore;
    private readonly MullaiTaskRuntimeOptions _runtimeOptions;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IWorkflowOutputDispatcher _workflowOutputDispatcher;
    private readonly ILogger<MullaiTaskWorkerService> _logger;

    public MullaiTaskWorkerService(
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IMullaiTaskExecutor executor,
        IMullaiTaskResponseChannel responseChannel,
        IWorkflowRunEventStore runEventStore,
        IWorkflowRegistry workflowRegistry,
        IWorkflowOutputDispatcher workflowOutputDispatcher,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        ILogger<MullaiTaskWorkerService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _responseChannel = responseChannel ?? throw new ArgumentNullException(nameof(responseChannel));
        _runEventStore = runEventStore ?? throw new ArgumentNullException(nameof(runEventStore));
        _workflowRegistry = workflowRegistry ?? throw new ArgumentNullException(nameof(workflowRegistry));
        _workflowOutputDispatcher = workflowOutputDispatcher ?? throw new ArgumentNullException(nameof(workflowOutputDispatcher));
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
        var workflowId = GetWorkflowId(workItem);

        try
        {
            _logger.LogInformation(
                "Worker {WorkerId} processing task {TaskId} (Attempt {Attempt}/{MaxAttempts})",
                workerId,
                workItem.TaskId,
                workItem.Attempt + 1,
                workItem.MaxAttempts);

            await _statusStore.MarkRunningAsync(workItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            await AppendRunEventAsync(
                workflowId,
                workItem.TaskId,
                "input",
                new
                {
                    prompt = workItem.Prompt,
                    source = workItem.Source.ToString(),
                    metadata = workItem.Metadata
                },
                cancellationToken).ConfigureAwait(false);
            using var scope = MullaiTaskExecutionContext.BeginScope(workItem.TaskId, workItem.SessionKey);
            var response = await _executor.ExecuteAsync(
                workItem,
                async responseSoFar =>
                {
                    await _statusStore.MarkRunningAsync(workItem, responseSoFar, cancellationToken).ConfigureAwait(false);
                    await AppendRunEventAsync(
                        workflowId,
                        workItem.TaskId,
                        "response_fragment",
                        new
                        {
                            response = responseSoFar
                        },
                        cancellationToken).ConfigureAwait(false);
                    var feedItem = new TaskResponseFeedItem
                    {
                        TaskId = workItem.TaskId,
                        SessionKey = workItem.SessionKey,
                        Response = responseSoFar
                    };
                    await _responseChannel.Writer.WriteAsync(feedItem, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            await _statusStore.MarkSucceededAsync(workItem, response, cancellationToken).ConfigureAwait(false);
            await AppendRunEventAsync(
                workflowId,
                workItem.TaskId,
                "response_final",
                new
                {
                    response
                },
                cancellationToken).ConfigureAwait(false);
            await DispatchWorkflowOutputsAsync(workItem, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            await AppendRunEventAsync(
                workflowId,
                workItem.TaskId,
                "error",
                new
                {
                    message = ex.Message,
                    exception = ex.ToString()
                },
                cancellationToken).ConfigureAwait(false);
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

    private async Task AppendRunEventAsync(
        string? workflowId,
        string taskId,
        string eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
        {
            return;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var runEvent = new WorkflowRunEvent
        {
            TaskId = taskId,
            WorkflowId = workflowId,
            EventType = eventType,
            PayloadJson = json,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await _runEventStore.AppendAsync(runEvent, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetWorkflowId(MullaiTaskWorkItem workItem)
    {
        if (workItem.Metadata is null)
        {
            return null;
        }

        return workItem.Metadata.TryGetValue("workflowId", out var workflowId) &&
               !string.IsNullOrWhiteSpace(workflowId)
            ? workflowId
            : null;
    }

    private async Task DispatchWorkflowOutputsAsync(
        MullaiTaskWorkItem workItem,
        string response,
        CancellationToken cancellationToken)
    {
        if (workItem.Metadata is null ||
            !workItem.Metadata.TryGetValue("workflowId", out var workflowId) ||
            string.IsNullOrWhiteSpace(workflowId))
        {
            return;
        }

        var definition = _workflowRegistry.GetById(workflowId);
        if (definition is null || definition.Outputs.Count == 0)
        {
            return;
        }

        await AppendRunEventAsync(
            workflowId,
            workItem.TaskId,
            "outputs_dispatching",
            new
            {
                outputs = definition.Outputs.Select(output => new
                {
                    output.Type,
                    output.Target,
                    output.Enabled,
                    output.Properties
                })
            },
            cancellationToken).ConfigureAwait(false);

        var context = new WorkflowOutputContext
        {
            Definition = definition,
            Response = response,
            TaskId = workItem.TaskId,
            SessionKey = workItem.SessionKey,
            Metadata = workItem.Metadata
        };

        await _workflowOutputDispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
    }
}
