namespace Mullai.Abstractions.Execution;

public interface IAgentActor
{
    string AgentName { get; }
    Task SendAsync(TaskExecutionRequest request, CancellationToken cancellationToken = default);
    Task StopAsync();
}
