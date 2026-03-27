using System.Collections.Concurrent;
using Mullai.Abstractions.Models;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services.Storage.InMemory;

public class InMemoryMullaiTaskStatusStore : IMullaiTaskStatusStore
{
    private readonly ConcurrentDictionary<string, MullaiTaskStatusSnapshot> _status = new();

    public Task MarkQueuedAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Queued);
        return Task.CompletedTask;
    }

    public Task MarkRunningAsync(MullaiTaskWorkItem workItem, string? response = null, MullaiUsage? usage = null, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Running, response: response, usage: usage);
        return Task.CompletedTask;
    }

    public Task MarkRetryScheduledAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.RetryScheduled, error: error);
        return Task.CompletedTask;
    }

    public Task MarkSucceededAsync(MullaiTaskWorkItem workItem, string response, MullaiUsage? usage = null, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Succeeded, response: response, usage: usage);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Failed, error: error);
        return Task.CompletedTask;
    }

    public Task<MullaiTaskStatusSnapshot?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _status.TryGetValue(taskId, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyCollection<MullaiTaskStatusSnapshot>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var resolvedTake = Math.Max(1, take);
        var result = _status.Values
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Take(resolvedTake)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<MullaiTaskStatusSnapshot>>(result);
    }

    public Task<MullaiUsage> GetTotalUsageAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        var snapshots = _status.Values
            .Where(s => string.Equals(s.SessionKey, sessionKey, StringComparison.Ordinal));

        var input = snapshots.Sum(s => s.InputTokenCount);
        var output = snapshots.Sum(s => s.OutputTokenCount);
        var total = snapshots.Sum(s => s.TotalTokenCount);

        return Task.FromResult(new MullaiUsage(input, output, total));
    }

    private void Upsert(
        MullaiTaskWorkItem workItem,
        MullaiTaskState state,
        string? response = null,
        string? error = null,
        MullaiUsage? usage = null)
    {
        string? workflowId = null;
        workItem.Metadata?.TryGetValue("workflowId", out workflowId);
        var snapshot = new MullaiTaskStatusSnapshot
        {
            TaskId = workItem.TaskId,
            SessionKey = workItem.SessionKey,
            AgentName = workItem.AgentName,
            Source = workItem.Source,
            WorkflowId = workflowId,
            State = state,
            Attempt = workItem.Attempt,
            MaxAttempts = workItem.MaxAttempts,
            Response = response,
            Error = error,
            InputTokenCount = usage?.InputTokenCount ?? 0,
            OutputTokenCount = usage?.OutputTokenCount ?? 0,
            TotalTokenCount = usage?.TotalTokenCount ?? 0,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _status.AddOrUpdate(workItem.TaskId, snapshot, (_, _) => snapshot);
    }
}
