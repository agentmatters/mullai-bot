using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskStatusStore
{
    Task MarkQueuedAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default);
    Task MarkRunningAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default);
    Task MarkRetryScheduledAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default);
    Task MarkSucceededAsync(MullaiTaskWorkItem workItem, string response, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default);
    Task<MullaiTaskStatusSnapshot?> GetAsync(string taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MullaiTaskStatusSnapshot>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default);
}
