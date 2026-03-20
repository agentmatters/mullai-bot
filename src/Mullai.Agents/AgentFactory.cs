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

using Mullai.Abstractions.Agents;

namespace Mullai.Agents;

public class AgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public AgentFactory(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    public MullaiAgent GetAgent(string agentName)
    {
        // For backwards compatibility or static agents
        var definition = new AgentDefinition
        {
            Name = agentName,
            Instructions = agentName switch
            {
                "Assistant" => new Assistant().Instructions,
                "Joker" => new Joker().Instructions,
                "Orchestrator" => GetOrchestratorInstructions(),
                _ => "You are a helpful assistant."
            },
            Tools = agentName == "Assistant" ? new List<string> { "WeatherTool", "CliTool", "FileSystemTool", "WordTool" } : new List<string>()
        };

        return CreateAgent(definition);
    }

    private string GetOrchestratorInstructions()
    {
        return """
            You are a task planner for Mullai, an AI assistant fabric.
            Decompose the following user request into a set of discrete tasks.
            Respond ONLY with a JSON object representing a TaskGraph.
            
            JSON Schema:
            {
                "Nodes": [
                    {
                        "Id": "string",
                        "Description": "Detailed description of the task",
                        "AssignedAgent": "AgentName",
                        "AgentDefinition": {
                            "Name": "AgentName",
                            "Instructions": "Specialized instructions for this agent",
                            "Tools": ["WeatherTool", "CliTool", "FileSystemTool", "WordTool"],
                            "MemoryContexts": ["CurrentFolder"]
                        }
                    }
                ],
                "Edges": [
                    {
                        "FromId": "string",
                        "ToId": "string"
                    }
                ]
            }

            Available Tools: WeatherTool, CliTool, FileSystemTool, WordTool.
            Use AgentDefinition only if a specialized agent with specific instructions or a subset of tools is needed. Otherwise, use AssignedAgent: "Assistant".
            """;
    }

    public MullaiAgent CreateAgent(AgentDefinition definition)
    {
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();
        
        var mode = definition.Metadata.GetValueOrDefault("ExecutionMode")?.ToString();
        var toolNames = definition.Tools;
        
        // If no tools are specified, provide a default set for Agent and Team modes
        if ((toolNames == null || toolNames.Count == 0) && mode != "Chat")
        {
            toolNames = new List<string> { "WeatherTool", "CliTool", "FileSystemTool", "WordTool" };
        }
        
        var tools = mode == "Chat" ? new List<AITool>() : ResolveTools(toolNames!);

        var options = new ChatClientAgentOptions
        {
            Name = definition.Name,
            ChatOptions = new ChatOptions
            {
                Instructions = definition.Instructions,
                Tools = tools,
                AllowMultipleToolCalls = true
            },
            AIContextProviders = ResolveContextProviders(definition.MemoryContexts)
        };

        var agent = chatClient.AsAIAgent(options, _serviceProvider.GetRequiredService<ILoggerFactory>())
            .AsBuilder()
            .Use(_serviceProvider.GetRequiredService<FunctionCallingMiddleware>().InvokeAsync)
            .UseOpenTelemetry(
                sourceName: OpenTelemetrySettings.ServiceName, 
                configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();

        return new MullaiAgent(agent, chatClient);
    }

    private List<AITool> ResolveTools(List<string> toolNames)
    {
        var tools = new List<AITool>();
        if (toolNames == null) return tools;

        foreach (var name in toolNames)
        {
            switch (name)
            {
                case "WeatherTool":
                    tools.AddRange(_serviceProvider.GetRequiredService<WeatherTool>().AsAITools());
                    break;
                case "CliTool":
                    tools.AddRange(_serviceProvider.GetRequiredService<CliTool>().AsAITools());
                    break;
                case "FileSystemTool":
                    tools.AddRange(_serviceProvider.GetRequiredService<FileSystemTool>().AsAITools());
                    break;
                case "WordTool":
                    tools.AddRange(_serviceProvider.GetRequiredService<WordTool>().AsAITools());
                    break;
            }
        }
        return tools;
    }

    private List<AIContextProvider> ResolveContextProviders(List<string> contextNames)
    {
        var providers = new List<AIContextProvider>();
        // Default context
        providers.Add(_serviceProvider.GetRequiredService<CurrentFolderContext>());
        // Add other contexts if needed based on contextNames
        return providers;
    }
}