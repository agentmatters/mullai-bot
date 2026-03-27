namespace Mullai.TaskRuntime.Models;

using Mullai.Abstractions.Models;

public sealed record MullaiTaskExecutionResult(string Response, MullaiUsage? Usage);
