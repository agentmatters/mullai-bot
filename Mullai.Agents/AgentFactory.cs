using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Agents.Agents;
using Mullai.Tools.WeatherTool;

namespace Mullai.Agents;

public class AgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public AgentFactory(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    public AIAgent GetAgent(string agentName)
    {
        
        AIAgent agent;
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();
        
        switch (agentName)
        {
            case "Joker":

                var joker = new Joker();
                agent = chatClient.AsAIAgent(joker.Instructions, joker.Name);
                break;
            
            case "Assistant":

                var assistant = new Assistant();
                agent = chatClient.AsAIAgent(
                    instructions: assistant.Instructions, 
                    name: assistant.Name,
                    tools: [.. _serviceProvider.GetRequiredService<WeatherTool>().AsAITools()],
                    services: _serviceProvider,
                    loggerFactory: _serviceProvider.GetRequiredService<ILoggerFactory>());
                break;
            
            default:
                var defaultAgent = new Joker();
                agent = chatClient.AsAIAgent(defaultAgent.Instructions, defaultAgent.Name);
                break;
        }

        return agent;
    }
}