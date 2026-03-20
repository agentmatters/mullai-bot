using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;
using Mullai.Abstractions.Persistence;

namespace Mullai.Orchestration;

public class ConversationManager : IConversationManager
{
    private readonly AgentFactory _agentFactory;
    private readonly IStateStore _stateStore;
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public ConversationManager(AgentFactory agentFactory, IStateStore stateStore)
    {
        _agentFactory = agentFactory;
        _stateStore = stateStore;
    }

    public async Task<AgentSession> CreateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var agent = _agentFactory.GetAgent("Assistant");
        var session = await agent.CreateSessionAsync(cancellationToken);
        _sessions[sessionId] = session;
        
        // Check if history already exists to avoid wiping it
        var existingHistory = await _stateStore.GetHistoryAsync(sessionId, cancellationToken);
        if (existingHistory == null || existingHistory.Count == 0)
        {
            await _stateStore.SaveHistoryAsync(sessionId, new List<ChatMessage>(), cancellationToken);
        }
        
        return session;
    }

    public Task<AgentSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult<AgentSession?>(session);
    }

    public async IAsyncEnumerable<ChatMessage> GetHistoryAsync(string sessionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = await _stateStore.GetHistoryAsync(sessionId, cancellationToken);
        foreach (var message in history)
        {
            yield return message;
        }
    }

    public Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        // This is a bit inefficient (load/save), but works for the shim
        return Task.Run(async () => {
            var history = await _stateStore.GetHistoryAsync(sessionId, cancellationToken);
            history.Add(message);
            await _stateStore.SaveHistoryAsync(sessionId, history, cancellationToken);
        }, cancellationToken);
    }

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return _stateStore.ClearSessionAsync(sessionId, cancellationToken);
    }
}
