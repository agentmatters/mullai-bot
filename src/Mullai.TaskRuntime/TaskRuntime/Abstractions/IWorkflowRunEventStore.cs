using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IWorkflowRunEventStore
{
    Task AppendAsync(WorkflowRunEvent runEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowRunEvent>> GetForTaskAsync(
        string taskId,
        int take = 200,
        CancellationToken cancellationToken = default);

    Task RemoveByTaskIdsAsync(IEnumerable<string> taskIds, CancellationToken cancellationToken = default);
}