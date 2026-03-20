using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Abstractions.Orchestration;

public interface IConversationManager
{
    Task<AgentSession> CreateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<AgentSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatMessage> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
