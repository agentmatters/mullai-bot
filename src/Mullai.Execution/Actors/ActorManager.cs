using System.Collections.Concurrent;
using Mullai.Abstractions.Execution;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Execution.Actors;

public class ActorManager : IActorManager
{
    private readonly ConcurrentDictionary<string, IAgentActor> _actors = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowEngine _workflow;

    public ActorManager(IServiceProvider serviceProvider, IWorkflowEngine workflow)
    {
        _serviceProvider = serviceProvider;
        _workflow = workflow;
    }

    public async Task DispatchAsync(TaskExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var agentName = request.Node.AssignedAgent ?? "Assistant";
        var actor = _actors.GetOrAdd(agentName, name => 
        {
            return new AgentActor(
                name,
                _serviceProvider.GetRequiredService<AgentFactory>(),
                _serviceProvider.GetRequiredService<IConversationManager>(),
                _serviceProvider.GetRequiredService<IEventBus>(),
                _workflow
            );
        });

        await actor.SendAsync(request, cancellationToken);
    }

    public async Task ShutdownAllAsync()
    {
        foreach (var actor in _actors.Values)
        {
            await actor.StopAsync();
        }
        _actors.Clear();
    }
}
