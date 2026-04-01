using System.Collections.Concurrent;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services.Storage.InMemory;

public sealed class InMemoryWorkflowOutputFailureStore : IWorkflowOutputFailureStore
{
    private readonly ConcurrentDictionary<string, WorkflowOutputFailure> _failures = new();

    public Task AddAsync(WorkflowOutputFailure failure, CancellationToken cancellationToken = default)
    {
        _failures[failure.Id] = failure;
        return Task.CompletedTask;
    }

    public Task<WorkflowOutputFailure?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        _failures.TryGetValue(id, out var failure);
        return Task.FromResult(failure);
    }

    public Task<IReadOnlyCollection<WorkflowOutputFailure>> GetRecentAsync(int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = _failures.Values
            .OrderByDescending(f => f.FailedAtUtc)
            .Take(Math.Max(1, take))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<WorkflowOutputFailure>>(result);
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _failures.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}