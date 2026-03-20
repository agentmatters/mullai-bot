using Microsoft.Extensions.AI;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Abstractions.Persistence;

public interface IStateStore
{
    Task SaveHistoryAsync(string sessionId, List<ChatMessage> history, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
    
    Task SaveCheckpointAsync(string sessionId, TaskGraph graph, CancellationToken cancellationToken = default);
    Task<TaskGraph?> GetCheckpointAsync(string sessionId, CancellationToken cancellationToken = default);

    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
