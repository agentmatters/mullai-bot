using System.Collections.Concurrent;
using Mullai.Abstractions.Clients;
using Mullai.Agents;
using Mullai.TaskRuntime.Abstractions;

namespace Mullai.TaskRuntime.Clients;

public class WebMullaiClientFactory : IMullaiTaskClientFactory
{
    private readonly AgentFactory _agentFactory;
    private readonly ConcurrentDictionary<string, IMullaiClient> _clients = new();

    public WebMullaiClientFactory(AgentFactory agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    public IMullaiClient GetClient(string sessionKey, string agentName)
    {
        var resolvedSessionKey = string.IsNullOrWhiteSpace(sessionKey) ? "default" : sessionKey.Trim();
        var resolvedAgentName = string.IsNullOrWhiteSpace(agentName) ? "Assistant" : agentName.Trim();
        var cacheKey = $"{resolvedAgentName}::{resolvedSessionKey}";

        return _clients.GetOrAdd(
            cacheKey,
            _ => new WebMullaiClient(_agentFactory, resolvedAgentName));
    }
}
