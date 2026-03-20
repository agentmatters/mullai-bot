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
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _taskWaiters = new();
    private readonly ConcurrentDictionary<string, TaskNode> _allTasks = new();

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
            Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Received status update for task {e.TaskId}: {e.Status}");
            
            if (_allTasks.TryGetValue(e.TaskId, out var task))
            {
                task.Status = Enum.TryParse<Mullai.Abstractions.Orchestration.TaskStatus>(e.Status, out var status) ? status : task.Status;
            }

            if (e.Status == "Completed" || e.Status == "Failed" || e.Status == "Cancelled")
            {
                if (e.Status == "Completed")
                {
                    _completedTasks.Add(e.TaskId);
                }
                
                _traceCounters.AddOrUpdate(e.SessionId, 0, (_, c) => Math.Max(0, c - 1));
                
                if (_taskWaiters.TryRemove(e.TaskId, out var tcs))
                {
                    Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Signaling waiter for task {e.TaskId} with status {e.Status}");
                    tcs.TrySetResult();
                }

                await CheckAndReleaseTasksAsync(e.SessionId);
            }
        }
    }

    public async Task SubmitGraphAsync(IEnumerable<TaskNode> nodes, string sessionId)
    {
        var nodeList = nodes.ToList();
        Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Submitting {nodeList.Count} nodes for session {sessionId}");
        foreach (var node in nodeList) 
        { 
            node.SessionId = sessionId;
            _pendingTasks.TryAdd(node.Id, node); 
            _allTasks.TryAdd(node.Id, node);
            if (!string.IsNullOrEmpty(node.TraceId))
            {
                _traceCounters.AddOrUpdate(node.TraceId, 1, (_, c) => c + 1);
            }
        }

        // Notify UI/CLI about new tasks
        await _eventBus.PublishAsync(new GraphCreatedEvent(sessionId, new TaskGraph { Nodes = nodeList }));

        await CheckAndReleaseTasksAsync(sessionId);
    }

    public bool IsTraceComplete(string traceId) => _traceCounters.TryGetValue(traceId, out var count) && count == 0;
    
    public Task<TaskNode?> GetTaskAsync(string taskId)
    {
        _allTasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    public Task<IEnumerable<TaskNode>> GetTasksAsync(string sessionId)
    {
        var tasks = _allTasks.Values.Where(t => t.SessionId == sessionId).ToList();
        return Task.FromResult<IEnumerable<TaskNode>>(tasks);
    }

    public async Task WaitForTaskAsync(string taskId, CancellationToken ct = default)
    {
        if (_completedTasks.Contains(taskId)) return;

        var tcs = _taskWaiters.GetOrAdd(taskId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            await tcs.Task;
        }
    }

    public async Task ApproveTaskAsync(string taskId, string sessionId) 
    { 
        _approvedTasks.Add(taskId); 
        await CheckAndReleaseTasksAsync(sessionId); 
    }

    private async Task CheckAndReleaseTasksAsync(string sessionId)
    {
        var readyTasks = _pendingTasks.Values
            .Where(t => t.Status == Mullai.Abstractions.Orchestration.TaskStatus.Pending)
            .ToList();

        if (readyTasks.Any())
        {
            Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Found {readyTasks.Count} ready tasks to release");
        }

        foreach (var task in readyTasks)
        {
            if (task.RequiresApproval && !_approvedTasks.Contains(task.Id)) 
            { 
                 Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Task {task.Id} requires approval");
                 await _eventBus.PublishAsync(new ApprovalRequestedEvent(task.Id, sessionId, task.Description)); 
                 continue; 
            }
            
            if (_pendingTasks.TryRemove(task.Id, out _)) 
            {
                Console.WriteLine($"[DEBUG: FLOW] WorkflowEngine: Releasing task {task.Id} to scheduler");
                await _scheduler.SubmitAsync(task, sessionId);
            }
        }
    }

    public async Task ExecuteAsync(TaskGraph graph, string sessionId, CancellationToken ct = default)
    {
        await SubmitGraphAsync(graph.Nodes, sessionId);
    }

    public async IAsyncEnumerable<WorkflowUpdate> ExecuteStreamingAsync(TaskGraph graph, string sessionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await SubmitGraphAsync(graph.Nodes, sessionId);
        
        // Listen for updates related to this graph
        await foreach (var e in _eventBus.SubscribeAsync<TaskStatusEvent>(ct))
        {
            if (e.SessionId == sessionId && graph.Nodes.Any(n => n.Id == e.TaskId))
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
