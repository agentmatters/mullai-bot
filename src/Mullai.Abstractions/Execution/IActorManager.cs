namespace Mullai.Abstractions.Execution;

public interface IActorManager
{
    Task DispatchAsync(TaskExecutionRequest request, CancellationToken cancellationToken = default);
}
