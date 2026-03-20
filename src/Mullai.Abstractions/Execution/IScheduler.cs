using Mullai.Abstractions.Orchestration;

namespace Mullai.Abstractions.Execution;

public interface IScheduler
{
    ValueTask SubmitAsync(TaskNode node, string sessionId, CancellationToken cancellationToken = default);
    ValueTask<TaskExecutionRequest?> DequeueAsync(CancellationToken cancellationToken = default);
}

public record TaskExecutionRequest(TaskNode Node, string SessionId);
