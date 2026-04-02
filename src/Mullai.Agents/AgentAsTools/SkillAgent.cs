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

namespace Mullai.Agents.AgentAsTools;

public static class SkillAgent
{
    public const string Name = "SkillAgent";
    public const string Instructions = """
                                       You are a specialized agent designed to manage and execute complex skill-based tasks. 
                                       Use the available tools to satisfy user requests efficiently.
                                       """;

    public static MullaiAgent Create(IServiceProvider serviceProvider)
    {
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();

        var functionCallingMiddleware = serviceProvider.GetRequiredService<FunctionCallingMiddleware>();

        var chatClientWithInjection = new ChatClientToolInjectionMiddleware(
            chatClient, [], functionCallingMiddleware.InvokeAsync);

        var agent = chatClientWithInjection.AsAIAgent(
                new ChatClientAgentOptions
                {
                    ChatOptions = new ChatOptions
                    {
                        Instructions = Instructions,
                        Tools = [],
                        AllowMultipleToolCalls = true
                    },
                    Name = Name,
                    AIContextProviders =
                    [
                        serviceProvider.GetRequiredKeyedService<AgentSkillsProvider>("skill-advertise")
                    ]
                },
                serviceProvider.GetRequiredService<ILoggerFactory>())
            .AsBuilder()
            .Use(ToolCallDynamicInjectionMiddleware.Create([]))
            .Use(functionCallingMiddleware.InvokeAsync)
            .UseOpenTelemetry(
                OpenTelemetrySettings.ServiceName,
                cfg => cfg.EnableSensitiveData = true)
            .Build();

        chatClientWithInjection.SetAgent(agent);

        return new MullaiAgent(agent, chatClient);
    }
}