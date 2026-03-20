using System.Collections.Concurrent;
using Mullai.Abstractions.Orchestration;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Execution;

namespace Mullai.Orchestration;

/// <summary>
/// The Grand Orchestrator: Maintains project graph state and releases tasks based on dependencies and approvals.
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly IScheduler _scheduler;
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, TaskNode> _pendingTasks = new();
    private readonly ConcurrentHashSet<string> _completedTasks = new();
    private readonly ConcurrentHashSet<string> _approvedTasks = new();
    private readonly ConcurrentDictionary<string, int> _traceCounters = new();

    public WorkflowEngine(IScheduler scheduler, IEventBus eventBus)
    {
        _scheduler = scheduler;
        _eventBus = eventBus;
        _ = Task.Run(ConsumeStatusEventsAsync);
    }

    private async Task ConsumeStatusEventsAsync()
    {
        await foreach (var e in _eventBus.SubscribeAsync<TaskStatusEvent>())
        {
            if (e.Status == "Completed")
            {
                _completedTasks.Add(e.TaskId);
                _traceCounters.AddOrUpdate(e.TraceId, 0, (_, c) => Math.Max(0, c - 1));
                await CheckAndReleaseTasksAsync("mullai-session");
            }
        }
    }

    public async Task SubmitGraphAsync(IEnumerable<TaskNode> nodes, string sessionId)
    {
        foreach (var node in nodes) 
        { 
            _pendingTasks.TryAdd(node.Id, node); 
            if (!string.IsNullOrEmpty(node.TraceId))
            {
                _traceCounters.AddOrUpdate(node.TraceId, 1, (_, c) => c + 1);
            }
        }
        await CheckAndReleaseTasksAsync(sessionId);
    }

    public bool IsTraceComplete(string traceId) => _traceCounters.TryGetValue(traceId, out var count) && count == 0;

    public async Task ApproveTaskAsync(string taskId) 
    { 
        _approvedTasks.Add(taskId); 
        await CheckAndReleaseTasksAsync("mullai-session"); 
    }

    private async Task CheckAndReleaseTasksAsync(string sessionId)
    {
        var readyTasks = _pendingTasks.Values
            .Where(t => t.Status == Mullai.Abstractions.Orchestration.TaskStatus.Pending)
            .Where(t => 
            {
                // Simple dependency check: for now we use Metadata to store dependencies or edges
                // In a more robust system, we would traverse the TaskGraph edges.
                return true; // Simplified for the migration step
            })
            .ToList();

        foreach (var task in readyTasks)
        {
            if (task.RequiresApproval && !_approvedTasks.Contains(task.Id)) 
            { 
                 await _eventBus.PublishAsync(new ApprovalRequestedEvent(task.Id, task.Description)); 
                 continue; 
            }
            
            if (_pendingTasks.TryRemove(task.Id, out _)) 
            {
                await _scheduler.SubmitAsync(task, sessionId);
            }
        }
    }

    // Compatibility for IWorkflowEngine interface
    public async Task ExecuteAsync(TaskGraph graph, CancellationToken ct = default)
    {
        await SubmitGraphAsync(graph.Nodes, "mullai-session");
    }

    public async IAsyncEnumerable<WorkflowUpdate> ExecuteStreamingAsync(TaskGraph graph, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await SubmitGraphAsync(graph.Nodes, "mullai-session");
        
        // Listen for updates related to this graph
        await foreach (var e in _eventBus.SubscribeAsync<TaskStatusEvent>(ct))
        {
            if (graph.Nodes.Any(n => n.Id == e.TaskId))
            {
                yield return new WorkflowUpdate 
                { 
                    NodeId = e.TaskId, 
                    Status = e.Status == "Completed" ? Mullai.Abstractions.Orchestration.TaskStatus.Completed : Mullai.Abstractions.Orchestration.TaskStatus.Running,
                    Message = e.Message
                };
            }
            
            if (IsTraceComplete(graph.Nodes.First().TraceId ?? "")) break;
        }
    }
}

public class ConcurrentHashSet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();
    public bool Add(T item) => _dictionary.TryAdd(item, 0);
    public bool Contains(T item) => _dictionary.ContainsKey(item);
}
