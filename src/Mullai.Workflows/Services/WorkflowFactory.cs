using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.Workflows.Services;

public sealed class WorkflowFactory : IWorkflowFactory
{
    public Workflow Build(WorkflowDefinition definition, IChatClient chatClient)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (chatClient is null)
        {
            throw new ArgumentNullException(nameof(chatClient));
        }

        return definition.Kind switch
        {
            WorkflowKind.SingleAgent => BuildSingleAgent(definition, chatClient),
            WorkflowKind.ParallelAgents => BuildParallelAgents(definition, chatClient),
            _ => throw new InvalidOperationException($"Unsupported workflow kind: {definition.Kind}.")
        };
    }

    private static Workflow BuildSingleAgent(WorkflowDefinition definition, IChatClient chatClient)
    {
        var agentDefinition = definition.Agents.FirstOrDefault()
            ?? throw new InvalidOperationException($"Workflow '{definition.Id}' requires one agent definition.");

        var agent = CreateAgent(agentDefinition, chatClient);
        return AgentWorkflowBuilder.BuildSequential(definition.Name, [agent]);
    }

    private static Workflow BuildParallelAgents(WorkflowDefinition definition, IChatClient chatClient)
    {
        if (definition.Agents.Count < 2)
        {
            throw new InvalidOperationException($"Workflow '{definition.Id}' requires at least two agents.");
        }

        var agents = definition.Agents
            .Select(agent => (AIAgent)CreateAgent(agent, chatClient))
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

    private static ChatClientAgent CreateAgent(WorkflowAgentDefinition definition, IChatClient chatClient)
    {
        var name = string.IsNullOrWhiteSpace(definition.Name)
            ? definition.DisplayName
            : definition.Name;
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? "WorkflowAgent"
            : name.Trim();

        return new ChatClientAgent(chatClient, instructions: definition.Instructions, name: resolvedName);
    }

    
}
