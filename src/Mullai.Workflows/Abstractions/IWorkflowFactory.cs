using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Mullai.Workflows.Models;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowFactory
{
    Workflow Build(WorkflowDefinition definition, IChatClient chatClient);
}
