using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;

namespace Mullai.TaskRuntime;

public static class MullaiTaskEndpoints
{
    public static IEndpointRouteBuilder MapMullaiTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/mullai/tasks")
            .WithTags("Mullai Tasks")
            .DisableAntiforgery();

        group.MapPost("/", EnqueueTaskAsync);
        group.MapGet("/{taskId}", GetTaskStatusAsync);
        group.MapGet("/", GetRecentTasksAsync);

        return endpoints;
    }

    private static async Task<IResult> EnqueueTaskAsync(
        MullaiTaskSubmitRequest request,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Results.BadRequest("Prompt is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SessionKey))
        {
            return Results.BadRequest("SessionKey is required.");
        }

        var maxAttempts = request.MaxAttempts is > 0 ? request.MaxAttempts.Value : runtimeOptions.Value.DefaultMaxAttempts;
        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = request.SessionKey.Trim(),
            AgentName = string.IsNullOrWhiteSpace(request.AgentName) ? "Assistant" : request.AgentName.Trim(),
            Prompt = request.Prompt.Trim(),
            Source = request.Source,
            MaxAttempts = maxAttempts,
            Metadata = request.Metadata
        };

        await queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);

        return Results.Accepted($"/api/mullai/tasks/{workItem.TaskId}", new { workItem.TaskId });
    }

    private static async Task<IResult> GetTaskStatusAsync(
        string taskId,
        IMullaiTaskStatusStore statusStore,
        CancellationToken cancellationToken)
    {
        var status = await statusStore.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        return status is null ? Results.NotFound() : Results.Ok(status);
    }

    private static async Task<IResult> GetRecentTasksAsync(
        IMullaiTaskStatusStore statusStore,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await statusStore.GetRecentAsync(take, cancellationToken).ConfigureAwait(false);
        return Results.Ok(results);
    }
}
