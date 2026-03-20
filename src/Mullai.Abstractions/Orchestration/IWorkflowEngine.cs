namespace Mullai.Abstractions.Orchestration;

public interface IWorkflowEngine
{
    Task ExecuteAsync(TaskGraph graph, string sessionId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<WorkflowUpdate> ExecuteStreamingAsync(TaskGraph graph, string sessionId, CancellationToken cancellationToken = default);
    Task SubmitGraphAsync(IEnumerable<TaskNode> nodes, string sessionId);
    Task ApproveTaskAsync(string taskId, string sessionId);
    bool IsTraceComplete(string traceId);
    Task<TaskNode?> GetTaskAsync(string taskId);
    Task<IEnumerable<TaskNode>> GetTasksAsync(string sessionId);
    Task WaitForTaskAsync(string taskId, CancellationToken ct = default);
}

public class WorkflowUpdate
{
    public string NodeId { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
