using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Mullai.Abstractions.Messaging;
using Mullai.Channels.Api.Hubs;

namespace Mullai.Channels.Api.Messaging;

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
        var taskUpdates = Task.Run(async () => 
        {
            await foreach (var @event in _eventBus.SubscribeAsync<TaskStatusEvent>(stoppingToken))
            {
                await _hubContext.Clients.Group(@event.SessionId).SendAsync("OnTaskUpdate", @event.TaskId, @event.Status, @event.Message, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        var agentUpdates = Task.Run(async () => 
        {
            await foreach (var @event in _eventBus.SubscribeAsync<TokenReceivedEvent>(stoppingToken))
            {
                await _hubContext.Clients.Group(@event.SessionId).SendAsync("OnAgentToken", @event.TaskId, @event.AgentName, @event.Token, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        var toolCallUpdates = Task.Run(async () => 
        {
            await foreach (var @event in _eventBus.SubscribeAsync<ToolCallEvent>(stoppingToken))
            {
                await _hubContext.Clients.Group(@event.SessionId).SendAsync("OnToolCall", @event.Observation, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        var graphUpdates = Task.Run(async () => 
        {
            await foreach (var @event in _eventBus.SubscribeAsync<GraphCreatedEvent>(stoppingToken))
            {
                await _hubContext.Clients.Group(@event.SessionId).SendAsync("OnGraphCreated", @event.Graph, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        await Task.WhenAll(taskUpdates, agentUpdates, toolCallUpdates, graphUpdates);
    }
}
