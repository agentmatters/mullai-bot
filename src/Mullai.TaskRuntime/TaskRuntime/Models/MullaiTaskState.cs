namespace Mullai.TaskRuntime.Models;

public enum MullaiTaskState
{
    Queued = 0,
    Running = 1,
    RetryScheduled = 2,
    Succeeded = 3,
    Failed = 4
}
