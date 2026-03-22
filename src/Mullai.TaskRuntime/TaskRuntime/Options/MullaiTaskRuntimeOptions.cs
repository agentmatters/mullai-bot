namespace Mullai.TaskRuntime.Options;

public sealed class MullaiTaskRuntimeOptions
{
    public const string SectionName = "Mullai:TaskRuntime";

    public int QueueCapacity { get; set; } = 1000;
    public int WorkerCount { get; set; } = Math.Max(2, Environment.ProcessorCount);
    public int DefaultMaxAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 3;
}
