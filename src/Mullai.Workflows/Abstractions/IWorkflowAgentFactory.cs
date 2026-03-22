using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Workflows.Abstractions;

public interface IWorkflowAgentFactory
{
    AIAgent CreateAgent(string workflowId, IChatClient chatClient);
}
