using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskQueue
{
    ValueTask EnqueueAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default);
    ValueTask<MullaiTaskWorkItem> DequeueAsync(CancellationToken cancellationToken = default);
}
