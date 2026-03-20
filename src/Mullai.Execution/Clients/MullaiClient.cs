using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Clients;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;

namespace Mullai.Execution.Clients;

public class MullaiClient : IMullaiClient
{
    private readonly AgentFactory _agentFactory;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IEventBus _eventBus;
    private readonly IConversationManager _conversationManager;
    private string _sessionId = "default";

    public MullaiClient(
        AgentFactory agentFactory,
        IWorkflowEngine workflowEngine,
        IEventBus eventBus,
        IConversationManager conversationManager)
    {
        _agentFactory = agentFactory;
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
        Console.WriteLine($"[DEBUG: FLOW] MullaiClient: Received prompt: {input}");
        await _conversationManager.AddMessageAsync(_sessionId, new ChatMessage(ChatRole.User, input), ct);

        // 2. Start root agent task
        var rootTask = new TaskNode
        {
            Id = "root-" + Guid.NewGuid().ToString()[..4],
            Description = input,
            AssignedAgent = "Assistant",
            Priority = 10,
            TraceId = _sessionId,
            Metadata = { ["SessionId"] = _sessionId }
        };

        // 3. Submit to workflow engine to kick off the agentic flow
        Console.WriteLine($"[DEBUG: FLOW] MullaiClient: Submitting root task {rootTask.Id} to WorkflowEngine");
        await _workflowEngine.SubmitGraphAsync(new[] { rootTask }, _sessionId);
    }

    public async IAsyncEnumerable<MullaiUpdate> GetUpdatesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var @event in _eventBus.SubscribeAllAsync(ct))
        {
            if (@event is ISessionEvent sessionEvent && sessionEvent.SessionId != _sessionId)
                continue;

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

    public async Task<List<Microsoft.Extensions.AI.ChatMessage>> GetHistoryAsync(CancellationToken ct = default)
    {
        var history = new List<Microsoft.Extensions.AI.ChatMessage>();
        await foreach (var message in _conversationManager.GetHistoryAsync(_sessionId, ct))
        {
            history.Add(message);
        }
        return history;
    }
}
