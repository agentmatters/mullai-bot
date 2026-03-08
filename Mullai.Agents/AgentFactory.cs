using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Agents.Agents;
using Mullai.Global.Config.OpenTelemetry;
using Mullai.Tools.WeatherTool;
using Mullai.Memory.UserMemory;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Middleware.Middlewares;

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
        var userMemory = _serviceProvider.GetRequiredService<UserInfoMemory>();
        
        switch (agentName)
        {
            case "Joker":

                var joker = new Joker();
                agent = chatClient.AsAIAgent(joker.Instructions, joker.Name);
                break;
            
            case "Assistant":

                var assistant = new Assistant();
                agent = chatClient.AsAIAgent(
                    new ChatClientAgentOptions()
                    {
                        ChatOptions = new()
                        {
                            Instructions = assistant.Instructions,
                            Tools = [
                                .. _serviceProvider.GetRequiredService<WeatherTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<CliTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<FileSystemTool>().AsAITools(),
                            ],
                            
                        },
                        Name = assistant.Name,
                        AIContextProviders = [
                            // userMemory,
                            _serviceProvider.GetRequiredService<FileAgentSkillsProvider>()
                        ],
                    },
                     _serviceProvider.GetRequiredService<ILoggerFactory>())
                    .AsBuilder()
                    .Use(new FunctionCallingMiddleware(_serviceProvider.GetRequiredService<ILogger<FunctionCallingMiddleware>>()).InvokeAsync)
                    .UseOpenTelemetry(
                        sourceName: OpenTelemetrySettings.ServiceName, 
                        configure: (cfg) => cfg.EnableSensitiveData = true)
                    .Build();
                break;
            
            default:
                var defaultAgent = new Joker();
                agent = chatClient.AsAIAgent(defaultAgent.Instructions, defaultAgent.Name);
                break;
        }

        return agent;
    }
}