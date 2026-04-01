using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IWorkflowOutputFailureStore
{
    Task AddAsync(WorkflowOutputFailure failure, CancellationToken cancellationToken = default);
    Task<WorkflowOutputFailure?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowOutputFailure>> GetRecentAsync(int take = 50,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}