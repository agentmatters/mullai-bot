using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mullai.Tools.Registry;
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
using Mullai.Tools.WorkflowStateTool;
using Mullai.Tools.RestApiTool;
using Mullai.Tools.HtmlToMarkdownTool;
using Mullai.Workflows.Abstractions;
using Mullai.Agents.Middlewares;

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
        List<AITool>? agentTools = null;

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
            case "Assistant":
                var assistant = new Assistant();
                
                agentTools = new List<AITool>();
                agentTools.AddRange(_serviceProvider.GetRequiredService<FileSystemTool>().AsAITools());
                agentTools.AddRange(_serviceProvider.GetRequiredService<BashTool>().AsAITools());

                var dynamicLoaderLogger = _serviceProvider.GetRequiredService<ILogger<DynamicToolLoader>>();
                var dynamicLoader = new DynamicToolLoader(_serviceProvider, agentTools, dynamicLoaderLogger);
                agentTools.AddRange(dynamicLoader.AsAITools());

                var functionCallingMiddleware = _serviceProvider.GetRequiredService<FunctionCallingMiddleware>();

                // IChatClient-level middleware: merges newly loaded tools on each LLM call within
                // the tool loop, wrapping them as ObservableAIFunction for middleware callback support
                var chatClientWithInjection = new ChatClientToolInjectionMiddleware(
                    chatClient, agentTools, functionCallingMiddleware.InvokeAsync);

                agent = chatClientWithInjection.AsAIAgent(
                    new ChatClientAgentOptions()
                    {
                        ChatOptions = new()
                        {
                            Instructions = assistant.Instructions,
                            Tools = agentTools,
                            AllowMultipleToolCalls = true
                        },
                        Name = assistant.Name,
                        AIContextProviders = [
                            _serviceProvider.GetRequiredService<CurrentFolderContext>(),
                        ],
                    },
                    _serviceProvider.GetRequiredService<ILoggerFactory>())
                    .AsBuilder()
                    // Outermost: inject latest session tools into run options BEFORE wrapping
                    .Use(ToolCallDynamicInjectionMiddleware.Create(agentTools))
                    // Middle: wraps tools as MiddlewareEnabledFunction + fires FunctionCallingMiddleware callback
                    .Use(functionCallingMiddleware.InvokeAsync)
                    .UseOpenTelemetry(
                        sourceName: OpenTelemetrySettings.ServiceName, 
                        configure: (cfg) => cfg.EnableSensitiveData = true)
                    .Build();

                // Set the agent reference now that it's built (deferred init)
                chatClientWithInjection.SetAgent(agent);
                break;
            
            default:
                var defaultAgent = new Joker();
                agent = chatClient.AsAIAgent(defaultAgent.Instructions, defaultAgent.Name);
                break;
        }

        return new MullaiAgent(agent, chatClient);
    }
}
