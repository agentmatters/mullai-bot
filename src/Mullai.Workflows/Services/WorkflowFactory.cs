using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mullai.Middleware.Middlewares;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.Workflows.Services;

public sealed class WorkflowFactory : IWorkflowFactory
{
    private readonly FunctionCallingMiddleware _functionCallingMiddleware;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWorkflowToolsProvider _toolsProvider;

    public WorkflowFactory(
        IWorkflowToolsProvider toolsProvider,
        ILoggerFactory loggerFactory,
        FunctionCallingMiddleware functionCallingMiddleware)
    {
        _toolsProvider = toolsProvider ?? throw new ArgumentNullException(nameof(toolsProvider));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _functionCallingMiddleware = functionCallingMiddleware ??
                                     throw new ArgumentNullException(nameof(functionCallingMiddleware));
    }

    public Workflow Build(WorkflowDefinition definition, IChatClient chatClient)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));

        if (chatClient is null) throw new ArgumentNullException(nameof(chatClient));

        return definition.Kind switch
        {
            WorkflowKind.SingleAgent => BuildSingleAgent(definition, chatClient),
            WorkflowKind.ParallelAgents => BuildParallelAgents(definition, chatClient),
            _ => throw new InvalidOperationException($"Unsupported workflow kind: {definition.Kind}.")
        };
    }

    private Workflow BuildSingleAgent(WorkflowDefinition definition, IChatClient chatClient)
    {
        var agentDefinition = definition.Agents.FirstOrDefault()
                              ?? throw new InvalidOperationException(
                                  $"Workflow '{definition.Id}' requires one agent definition.");

        var agent = CreateAgent(agentDefinition, chatClient);
        return AgentWorkflowBuilder.BuildSequential(definition.Name, agent);
    }

    private Workflow BuildParallelAgents(WorkflowDefinition definition, IChatClient chatClient)
    {
        if (definition.Agents.Count < 2)
            throw new InvalidOperationException($"Workflow '{definition.Id}' requires at least two agents.");

        var agents = definition.Agents
            .Select(agent => CreateAgent(agent, chatClient))
            .ToArray();

        return AgentWorkflowBuilder.BuildConcurrent(
            definition.Name,
            agents,
            outputs =>
            {
                var combined = string.Join(Environment.NewLine,
                    outputs.SelectMany(list => list).Select(message => message.Text));
                return [new ChatMessage(ChatRole.Assistant, combined)];
            });
    }

    private AIAgent CreateAgent(WorkflowAgentDefinition definition, IChatClient chatClient)
    {
        var name = string.IsNullOrWhiteSpace(definition.Name)
            ? definition.DisplayName
            : definition.Name;
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? "WorkflowAgent"
            : name.Trim();

        var tools = _toolsProvider.GetTools().ToList();
        var agent = chatClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                Name = resolvedName,
                ChatOptions = new ChatOptions
                {
                    Instructions = definition.Instructions,
                    Tools = tools,
                    AllowMultipleToolCalls = true
                }
            },
            _loggerFactory);

        return agent
            .AsBuilder()
            .Use(_functionCallingMiddleware.InvokeAsync)
            .Build();
    }
}