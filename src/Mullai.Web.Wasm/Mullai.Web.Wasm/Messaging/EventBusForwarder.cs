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
    
    public EventBusForwarder(IEventBus eventBus, IHubContext<FabricHub> hubContext)
    {
        _eventBus = eventBus;
        _hubContext = hubContext;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _eventBus.SubscribeAllAsync(stoppingToken))
        {
            var targetSessionId = @event switch
            {
                GraphCreatedEvent e => e.SessionId,
                TokenReceivedEvent e => e.SessionId,
                TaskStatusEvent e => e.SessionId,
                ToolCallEvent e => e.SessionId,
                _ => null
            };

            if (string.IsNullOrEmpty(targetSessionId)) continue;
            
            switch (@event)
            {
                case GraphCreatedEvent graphEvent:
                    await _hubContext.Clients.Group(targetSessionId).SendAsync(
                        "OnGraphCreated",
                        graphEvent.Graph,
                        cancellationToken: stoppingToken);
                    break;
                case TokenReceivedEvent tokenEvent:
                    await _hubContext.Clients.Group(targetSessionId).SendAsync(
                        "OnAgentToken",
                        tokenEvent.TaskId,
                        tokenEvent.AgentName,
                        tokenEvent.Token,
                        cancellationToken: stoppingToken);
                    break;
                case TaskStatusEvent statusEvent:
                    await _hubContext.Clients.Group(targetSessionId).SendAsync(
                        "OnTaskUpdate",
                        statusEvent.TaskId,
                        statusEvent.Status,
                        statusEvent.Message,
                        cancellationToken: stoppingToken);
                    break;
                case ToolCallEvent toolCallEvent:
                    await _hubContext.Clients.Group(targetSessionId).SendAsync(
                        "OnToolCall",
                        ToSnapshot(toolCallEvent.Observation),
                        cancellationToken: stoppingToken);
                    break;
            }
        }
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
