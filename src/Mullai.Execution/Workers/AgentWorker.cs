using Microsoft.Extensions.Hosting;
using Mullai.Abstractions.Execution;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;

namespace Mullai.Execution.Workers;

public class AgentWorker : BackgroundService
{
    private readonly IScheduler _scheduler;
    private readonly IActorManager _actorManager;
    private readonly IEventBus _eventBus;

    private readonly SemaphoreSlim _throttle = new(2); // Global slot limit

    public AgentWorker(
        IScheduler scheduler, 
        IActorManager actorManager,
        IEventBus eventBus)
    {
        _scheduler = scheduler;
        _actorManager = actorManager;
        _eventBus = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _scheduler.DequeueAsync(stoppingToken);
                if (request == null) continue;

                // --- CONCURRENCY THROTTLING ---
                await _eventBus.PublishAsync(new TaskStatusEvent(request.Node.Id, request.Node.TraceId ?? "", "Pending", "Waiting for free slot"), stoppingToken);
                await _throttle.WaitAsync(stoppingToken);
                
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _eventBus.PublishAsync(new TaskStatusEvent(request.Node.Id, request.Node.TraceId ?? "", "InProgress", "Dispatching to agent actor"), stoppingToken);
                        await _actorManager.DispatchAsync(request, stoppingToken);
                    }
                    finally
                    {
                        _throttle.Release();
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new TaskStatusEvent("System", "Error", "Error", ex.Message), stoppingToken);
            }
        }
    }
}
