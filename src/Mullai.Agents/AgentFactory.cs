using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Abstractions.Configuration;
using Mullai.Agents.Middlewares;
using Mullai.Memory.SystemContext;
using Mullai.Middleware.Middlewares;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.Tools.BashTool;
using Mullai.Tools.CliTool;
using Mullai.Tools.CodeSearchTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.HtmlToMarkdownTool;
using Mullai.Tools.Registry;
using Mullai.Tools.RestApiTool;
using Mullai.Tools.TodoTool;
using Mullai.Tools.WeatherTool;
using Mullai.Tools.WebTool;
using Mullai.Tools.WorkflowStateTool;
using Mullai.Tools.WorkflowTool;
using Mullai.Workflows.Abstractions;

namespace Mullai.Agents;

public class AgentFactory
{
    private const string WorkflowAgentPrefix = "workflow:";
    private readonly IServiceProvider _serviceProvider;

    public AgentFactory(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<MullaiAgent> GetAgent(string agentName)
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

        var configManager = _serviceProvider.GetRequiredService<IMullaiConfigurationManager>();
        var agents = configManager.GetAgents();

        var agentDef = agents.FirstOrDefault(a => a.Id.Equals(agentName, StringComparison.OrdinalIgnoreCase) ||
                                                  a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));

        // Use the first enabled agent as default if the requested name doesn't match
        if (agentDef == null)
            agentDef = agents.FirstOrDefault(a => a.Id == "assistant") ??
                       agents.FirstOrDefault(a => a.Enabled) ?? agents.FirstOrDefault();

        if (agentDef == null) throw new Exception($"Agent '{agentName}' not found and no defaults available.");

        agentTools = new List<AITool>();

        // Add default tools from definition
        foreach (var toolDef in agentDef.Tools.Where(t => t.IsDefault))
        {
            var tools = await ResolveTool(toolDef.Name, agentDef, agentTools, configManager);
            agentTools.AddRange(tools);
        }

        var skillAgent = _serviceProvider.GetRequiredKeyedService<MullaiAgent>("SkillAgent");
        
        agentTools.AddRange(skillAgent.AsAIFunction());
        
        var functionCallingMiddleware = _serviceProvider.GetRequiredService<FunctionCallingMiddleware>();

        // IChatClient-level middleware: merges newly loaded tools on each LLM call within
        // the tool loop, wrapping them as ObservableAIFunction for middleware callback support
        var chatClientWithInjection = new ChatClientToolInjectionMiddleware(
            chatClient, agentTools, functionCallingMiddleware.InvokeAsync);

        agent = chatClientWithInjection.AsAIAgent(
                new ChatClientAgentOptions
                {
                    ChatOptions = new ChatOptions
                    {
                        Instructions = agentDef.Instructions,
                        Tools = agentTools,
                        AllowMultipleToolCalls = true
                    },
                    Name = agentDef.Name,
                    AIContextProviders =
                    [
                        _serviceProvider.GetRequiredService<CurrentFolderContext>(),
                    ]
                },
                _serviceProvider.GetRequiredService<ILoggerFactory>())
            .AsBuilder()
            // Outermost: inject latest session tools into run options BEFORE wrapping
            .Use(ToolCallDynamicInjectionMiddleware.Create(agentTools))
            // Middle: wraps tools as MiddlewareEnabledFunction + fires FunctionCallingMiddleware callback
            .Use(functionCallingMiddleware.InvokeAsync)
            .UseOpenTelemetry(
                OpenTelemetrySettings.ServiceName,
                cfg => cfg.EnableSensitiveData = true)
            .Build();

        // Set the agent reference now that it's built (deferred init)
        chatClientWithInjection.SetAgent(agent);

        return new MullaiAgent(agent, chatClient);
    }

    private async Task<IEnumerable<AITool>> ResolveTool(string toolName, AgentDefinition agentDef,
        List<AITool> sessionTools, IMullaiConfigurationManager configManager)
    {
        var dynamicTools = agentDef.Tools.Where(t => !t.IsDefault).Select(t => t.Name).ToList();

        if (toolName.StartsWith("MCP:", StringComparison.OrdinalIgnoreCase))
        {
            var loader = new DynamicToolLoader(
                _serviceProvider,
                sessionTools,
                _serviceProvider.GetRequiredService<ILogger<DynamicToolLoader>>(),
                configManager,
                dynamicTools);
            return await loader.LoadMcpToolsAsync(toolName[4..]);
        }

        return toolName switch
        {
            "FileSystemTool" => _serviceProvider.GetRequiredService<FileSystemTool>().AsAITools(),
            "BashTool" => _serviceProvider.GetRequiredService<BashTool>().AsAITools(),
            "WeatherTool" => _serviceProvider.GetRequiredService<WeatherTool>().AsAITools(),
            "CliTool" => _serviceProvider.GetRequiredService<CliTool>().AsAITools(),
            "TodoTool" => _serviceProvider.GetRequiredService<TodoTool>().AsAITools(),
            "WebTool" => _serviceProvider.GetRequiredService<WebTool>().AsAITools(),
            "CodeSearchTool" => _serviceProvider.GetRequiredService<CodeSearchTool>().AsAITools(),
            "WorkflowTool" => _serviceProvider.GetRequiredService<WorkflowTool>().AsAITools(),
            "WorkflowStateTool" => _serviceProvider.GetRequiredService<WorkflowStateTool>().AsAITools(),
            "RestApiTool" => _serviceProvider.GetRequiredService<RestApiTool>().AsAITools(),
            "HtmlToMarkdownTool" => _serviceProvider.GetRequiredService<HtmlToMarkdownTool>().AsAITools(),
            "DynamicToolLoader" => new DynamicToolLoader(
                _serviceProvider,
                sessionTools,
                _serviceProvider.GetRequiredService<ILogger<DynamicToolLoader>>(),
                configManager,
                dynamicTools).AsAITools(),
            _ => Enumerable.Empty<AITool>()
        };
    }
}