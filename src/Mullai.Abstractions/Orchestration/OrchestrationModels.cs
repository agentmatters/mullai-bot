using Mullai.Abstractions.Agents;
using System.Collections.Generic;

namespace Mullai.Abstractions.Orchestration;

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Approved
}

public enum ExecutionMode
{
    Chat,
    Agent,
    Team
}

public class TaskNode
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignedAgent { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public bool RequiresApproval { get; set; } = false;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string TraceId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public AgentDefinition? AgentDefinition { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TaskEdge
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
}

public class TaskGraph
{
    public List<TaskNode> Nodes { get; set; } = new();
    public List<TaskEdge> Edges { get; set; } = new();
}
