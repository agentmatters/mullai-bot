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
                await _hubContext.Clients.All.SendAsync("OnTaskUpdate", @event.TaskId, @event.Status, @event.Message, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        var agentUpdates = Task.Run(async () => 
        {
            await foreach (var @event in _eventBus.SubscribeAsync<AgentUpdateEvent>(stoppingToken))
            {
                await _hubContext.Clients.All.SendAsync("OnAgentToken", @event.AgentName, @event.Content, cancellationToken: stoppingToken);
            }
        }, stoppingToken);

        await Task.WhenAll(taskUpdates, agentUpdates);
    }
}
