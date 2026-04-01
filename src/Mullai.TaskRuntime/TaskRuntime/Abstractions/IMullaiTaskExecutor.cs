using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskExecutor
{
    Task<MullaiTaskExecutionResult> ExecuteAsync(
        MullaiTaskWorkItem workItem,
        Func<string, Task>? onResponseFragment = null,
        CancellationToken cancellationToken = default);
}