using Mullai.Abstractions.Models;

namespace Mullai.TaskRuntime.Models;

public sealed record MullaiTaskExecutionResult(string Response, MullaiUsage? Usage);