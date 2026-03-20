using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;
using Mullai.Web.Wasm.Hubs;

namespace Mullai.Web.Wasm.Messaging;

public class EventBusForwarder : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IHubContext<FabricHub> _hubContext;
    private readonly ConcurrentDictionary<string, string> _taskToSession = new();
    private readonly ConcurrentDictionary<string, string> _traceToSession = new();

    public EventBusForwarder(IEventBus eventBus, IHubContext<FabricHub> hubContext)
    {
        _eventBus = eventBus;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _eventBus.SubscribeAllAsync(stoppingToken))
        {
            switch (@event)
            {
                case GraphCreatedEvent graphEvent:
                    RegisterGraph(graphEvent.Graph);
                    break;
                case TokenReceivedEvent tokenEvent:
                    if (TryResolveSession(tokenEvent.TaskId, null, out var tokenSession))
                    {
                        await _hubContext.Clients.Group(tokenSession).SendAsync(
                            "OnAgentToken",
                            tokenEvent.TaskId,
                            tokenEvent.AgentName,
                            tokenEvent.Token,
                            cancellationToken: stoppingToken);
                    }
                    break;
                case TaskStatusEvent statusEvent:
                    if (TryResolveSession(statusEvent.TaskId, statusEvent.TraceId, out var statusSession))
                    {
                        await _hubContext.Clients.Group(statusSession).SendAsync(
                            "OnTaskUpdate",
                            statusEvent.TaskId,
                            statusEvent.Status,
                            statusEvent.Message,
                            cancellationToken: stoppingToken);
                    }
                    break;
                case ToolCallEvent toolCallEvent:
                    if (TryResolveSession(toolCallEvent.Observation.TaskId, null, out var toolSession))
                    {
                        await _hubContext.Clients.Group(toolSession).SendAsync(
                            "OnToolCall",
                            ToSnapshot(toolCallEvent.Observation),
                            cancellationToken: stoppingToken);
                    }
                    break;
            }
        }
    }

    private void RegisterGraph(TaskGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            if (node.Metadata.TryGetValue("SessionId", out var rawSession) &&
                rawSession is string sessionId &&
                !string.IsNullOrWhiteSpace(sessionId))
            {
                _taskToSession[node.Id] = sessionId;
                if (!string.IsNullOrWhiteSpace(node.TraceId))
                {
                    _traceToSession[node.TraceId] = sessionId;
                }
            }
        }
    }

    private bool TryResolveSession(string? taskId, string? traceId, out string sessionId)
    {
        sessionId = string.Empty;
        if (!string.IsNullOrWhiteSpace(taskId) && _taskToSession.TryGetValue(taskId, out var taskSession))
        {
            sessionId = taskSession;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(traceId) && _traceToSession.TryGetValue(traceId, out var traceSession))
        {
            sessionId = traceSession;
            return true;
        }

        return false;
    }

    private static ToolCallSnapshot ToSnapshot(Mullai.Abstractions.Observability.ToolCallObservation observation)
    {
        return new ToolCallSnapshot
        {
            ToolName = observation.ToolName,
            Result = observation.Result,
            Error = observation.Error,
            StartedAt = observation.StartedAt,
            FinishedAt = observation.FinishedAt,
            TaskId = observation.TaskId,
            AgentName = observation.AgentName
        };
    }
}
