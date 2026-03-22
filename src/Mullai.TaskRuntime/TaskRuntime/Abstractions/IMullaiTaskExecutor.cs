using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskExecutor
{
    Task<string> ExecuteAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default);
}
