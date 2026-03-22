using Mullai.Abstractions.WorkflowState;

namespace Mullai.Abstractions.WorkflowState;

public interface IWorkflowStateStore
{
    Task<WorkflowStateRecord?> GetAsync(string workflowId, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkflowStateRecord>> GetAllAsync(string workflowId, CancellationToken cancellationToken = default);
    Task UpsertAsync(string workflowId, string key, string jsonValue, CancellationToken cancellationToken = default);
    Task RemoveAsync(string workflowId, string key, CancellationToken cancellationToken = default);
}
