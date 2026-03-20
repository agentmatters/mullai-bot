using Mullai.Abstractions.Agents;

using Microsoft.Extensions.AI;

namespace Mullai.Abstractions.Orchestration;

public enum ExecutionMode
{
    Chat,   // No tools, single agent
    Agent,  // Tools enabled, single agent
    Team    // Full orchestration (Smart/LLM planning)
}

public interface IPlanner
{
    Task<TaskGraph> PlanAsync(string userInput, IEnumerable<ChatMessage>? history = null, ExecutionMode mode = ExecutionMode.Team, CancellationToken cancellationToken = default);
}

public class TaskGraph
{
    public List<TaskNode> Nodes { get; set; } = new();
    public List<TaskEdge> Edges { get; set; } = new();
}

public class TaskNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string? AssignedAgent { get; set; }
    public string? TraceId { get; set; }
    public bool RequiresApproval { get; set; } = false;
    public int Priority { get; set; } = 0;
    public AgentDefinition? AgentDefinition { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TaskEdge
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
}

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}
