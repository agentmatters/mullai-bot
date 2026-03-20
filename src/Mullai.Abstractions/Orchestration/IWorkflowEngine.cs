namespace Mullai.Abstractions.Orchestration;

public interface IWorkflowEngine
{
    Task ExecuteAsync(TaskGraph graph, CancellationToken cancellationToken = default);
    IAsyncEnumerable<WorkflowUpdate> ExecuteStreamingAsync(TaskGraph graph, CancellationToken cancellationToken = default);
    Task SubmitGraphAsync(IEnumerable<TaskNode> nodes, string sessionId);
    Task ApproveTaskAsync(string taskId);
    bool IsTraceComplete(string traceId);
}

public class WorkflowUpdate
{
    public string NodeId { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
