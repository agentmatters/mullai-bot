using Mullai.Abstractions.Agents;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;

namespace Mullai.Orchestration;

public class AgentRouter : IAgentRouter
{
    private readonly AgentFactory _agentFactory;

    public AgentRouter(AgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public Task<IMullaiAgent> RouteAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
        // Simple routing logic: look for keywords or default to Assistant
        var agentName = "Assistant";
        
        if (taskDescription.Contains("joke", StringComparison.OrdinalIgnoreCase))
        {
            agentName = "Joker";
        }

        // In a real implementation, this could use an LLM or a more complex registry
        var mullaiAgent = _agentFactory.GetAgent(agentName);
        
        return Task.FromResult<IMullaiAgent>(mullaiAgent);
    }
}
