using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Mullai.Workflows.Abstractions;

namespace Mullai.Workflows.Services;

public sealed class WorkflowAgentFactory : IWorkflowAgentFactory
{
    private readonly IWorkflowRegistry _registry;
    private readonly IWorkflowFactory _workflowFactory;

    public WorkflowAgentFactory(IWorkflowRegistry registry, IWorkflowFactory workflowFactory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _workflowFactory = workflowFactory ?? throw new ArgumentNullException(nameof(workflowFactory));
    }

    public AIAgent CreateAgent(string workflowId, IChatClient chatClient)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow id is required.", nameof(workflowId));

        var definition = _registry.GetById(workflowId.Trim())
                         ?? throw new InvalidOperationException($"Workflow '{workflowId}' was not found.");

        var workflow = _workflowFactory.Build(definition, chatClient);
        var agentId = $"workflow-{definition.Id}";
        return workflow.AsAIAgent(agentId, definition.Name);
    }
}