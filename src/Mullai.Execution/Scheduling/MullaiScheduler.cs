using System.Threading.Channels;
using Mullai.Abstractions.Execution;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Execution.Scheduling;

public class MullaiScheduler : IScheduler
{
    private readonly Channel<TaskExecutionRequest> _queue = Channel.CreateUnbounded<TaskExecutionRequest>(new UnboundedChannelOptions 
    { 
        SingleReader = false, 
        SingleWriter = false 
    });

    public ValueTask SubmitAsync(TaskNode node, string sessionId, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(new TaskExecutionRequest(node, sessionId), cancellationToken);
    }

    public async ValueTask<TaskExecutionRequest?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
