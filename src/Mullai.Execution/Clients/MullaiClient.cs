using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Clients;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Execution.Clients;

public class MullaiClient : IMullaiClient
{
    private readonly IPlanner _planner;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IEventBus _eventBus;
    private readonly IConversationManager _conversationManager;
    private string _sessionId = "default";

    public MullaiClient(
        IPlanner planner,
        IWorkflowEngine workflowEngine,
        IEventBus eventBus,
        IConversationManager conversationManager)
    {
        _planner = planner;
        _workflowEngine = workflowEngine;
        _eventBus = eventBus;
        _conversationManager = conversationManager;
    }

    public async Task InitialiseAsync(string sessionId = "default", CancellationToken ct = default)
    {
        _sessionId = sessionId;
        await _conversationManager.GetSessionAsync(sessionId, ct);
    }

    public async Task SendPromptAsync(string input, ExecutionMode mode = ExecutionMode.Team, CancellationToken ct = default)
    {
        // 1. Save user input to history
        await _conversationManager.AddMessageAsync(_sessionId, new ChatMessage(ChatRole.User, input), ct);

        // 2. Load full history for planning
        var history = new List<ChatMessage>();
        await foreach (var msg in _conversationManager.GetHistoryAsync(_sessionId, ct))
        {
            history.Add(msg);
        }

        // 3. Plan with context
        var plan = await _planner.PlanAsync(input, history, mode, ct);
        foreach (var node in plan.Nodes)
        {
            node.Metadata["SessionId"] = _sessionId;
        }

        // 4. Notify UI about the new graph
        await _eventBus.PublishAsync(new GraphCreatedEvent(plan), ct);

        // 5. Submit for execution
        await _workflowEngine.SubmitGraphAsync(plan.Nodes, _sessionId);
    }

    public async IAsyncEnumerable<MullaiUpdate> GetUpdatesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var @event in _eventBus.SubscribeAllAsync(ct))
        {
            var update = @event switch
            {
                TokenReceivedEvent e => new MullaiUpdate { TaskId = e.TaskId, AgentName = e.AgentName, Text = e.Token, Type = UpdateType.Token },
                TaskStatusEvent e => new MullaiUpdate { TaskId = e.TaskId, Status = e.Status, Text = e.Message, Type = UpdateType.Status },
                ToolCallEvent e => new MullaiUpdate { ToolCall = e.Observation, Type = UpdateType.ToolCall },
                GraphCreatedEvent e => new MullaiUpdate { Graph = e.Graph, Type = UpdateType.Graph },
                _ => null
            };

            if (update != null) yield return update;
        }
    }
}
