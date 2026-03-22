using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Agents.Agents;
using Mullai.Memory.SystemContext;
using Mullai.Tools.WeatherTool;
using Mullai.Memory.UserMemory;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.WordTool;
using Mullai.Middleware.Middlewares;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.Tools.BashTool;
using Mullai.Tools.CodeSearchTool;
using Mullai.Tools.TodoTool;
using Mullai.Tools.WebTool;
using Mullai.Tools.WorkflowTool;
using Mullai.Workflows.Abstractions;

namespace Mullai.Agents;

public class AgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private const string WorkflowAgentPrefix = "workflow:";
    
    public AgentFactory(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    public MullaiAgent GetAgent(string agentName)
    {
        AIAgent agent;
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();

        if (!string.IsNullOrWhiteSpace(agentName) &&
            agentName.StartsWith(WorkflowAgentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var workflowId = agentName[WorkflowAgentPrefix.Length..].Trim();
            var workflowAgentFactory = _serviceProvider.GetRequiredService<IWorkflowAgentFactory>();
            agent = workflowAgentFactory.CreateAgent(workflowId, chatClient);
            return new MullaiAgent(agent, chatClient);
        }

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
                                .. _serviceProvider.GetRequiredService<BashTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<TodoTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<WebTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<CodeSearchTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<FileSystemTool>().AsAITools(),
                                .. _serviceProvider.GetRequiredService<WorkflowTool>().AsAITools(),
                                // .. _serviceProvider.GetRequiredService<WordTool>().AsAITools(),
                            ],
                            AllowMultipleToolCalls = true
                        },
                        Name = assistant.Name,
                        AIContextProviders = [
                            _serviceProvider.GetRequiredService<CurrentFolderContext>(),
                        ],
                    },
                    _serviceProvider.GetRequiredService<ILoggerFactory>())
                    .AsBuilder()
                    .Use(_serviceProvider.GetRequiredService<FunctionCallingMiddleware>().InvokeAsync)
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

        return new MullaiAgent(agent, chatClient);
    }
}
