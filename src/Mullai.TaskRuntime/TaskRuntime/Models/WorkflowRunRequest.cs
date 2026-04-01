namespace Mullai.TaskRuntime.Models;

public sealed class WorkflowRunRequest
{
    public string? Input { get; init; }
    public string? SessionKey { get; init; }
}