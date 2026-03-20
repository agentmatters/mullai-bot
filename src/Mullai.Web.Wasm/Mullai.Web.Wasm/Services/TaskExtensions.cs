using Mullai.Abstractions.Orchestration;

namespace Mullai.Web.Wasm.Services;

public static class TaskExtensions
{
    public static int CountActive(this Dictionary<string, TaskNode> tasks)
    {
        return tasks.Values.Count(t => t.Status == Mullai.Abstractions.Orchestration.TaskStatus.Running || t.Status == Mullai.Abstractions.Orchestration.TaskStatus.Pending);
    }
}
