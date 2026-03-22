using Microsoft.Extensions.Options;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;
using System.Text.Json;

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

        var workflowGroup = endpoints
            .MapGroup("/api/mullai/workflows")
            .WithTags("Mullai Workflows")
            .DisableAntiforgery();

        workflowGroup.MapGet("/", GetWorkflowsAsync);
        workflowGroup.MapGet("/{workflowId}", GetWorkflowAsync);
        workflowGroup.MapPost("/{workflowId}/run", RunWorkflowAsync);
        workflowGroup.MapPost("/{workflowId}/triggers/{triggerId}", RunWorkflowTriggerAsync);

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

    private static Task<IResult> GetWorkflowsAsync(IWorkflowRegistry registry)
    {
        var workflows = registry.GetAll();
        return Task.FromResult(Results.Ok(workflows));
    }

    private static Task<IResult> GetWorkflowAsync(string workflowId, IWorkflowRegistry registry)
    {
        var workflow = registry.GetById(workflowId);
        return Task.FromResult(workflow is null ? Results.NotFound() : Results.Ok(workflow));
    }

    private static async Task<IResult> RunWorkflowAsync(
        string workflowId,
        WorkflowRunRequest request,
        IWorkflowRegistry registry,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        CancellationToken cancellationToken)
    {
        if (registry.GetById(workflowId) is null)
        {
            return Results.NotFound($"Workflow '{workflowId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return Results.BadRequest("Input is required.");
        }

        var sessionKey = string.IsNullOrWhiteSpace(request.SessionKey)
            ? $"workflow-{workflowId}-{Guid.NewGuid():N}"
            : request.SessionKey.Trim();

        var maxAttempts = Math.Max(1, runtimeOptions.Value.DefaultMaxAttempts);
        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = sessionKey,
            AgentName = $"workflow:{workflowId}",
            Prompt = request.Input.Trim(),
            Source = MullaiTaskSource.Client,
            MaxAttempts = maxAttempts,
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = workflowId
            }
        };

        await queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);

        return Results.Accepted($"/api/mullai/tasks/{workItem.TaskId}", new { workItem.TaskId, workItem.SessionKey });
    }

    private static async Task<IResult> RunWorkflowTriggerAsync(
        string workflowId,
        string triggerId,
        JsonElement payload,
        HttpRequest httpRequest,
        IWorkflowRegistry registry,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        CancellationToken cancellationToken)
    {
        var workflow = registry.GetById(workflowId);
        if (workflow is null)
        {
            return Results.NotFound($"Workflow '{workflowId}' was not found.");
        }

        var trigger = workflow.Triggers.FirstOrDefault(t =>
            t.Enabled &&
            t.Type.Equals("webhook", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(t.Id, triggerId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(t.Name, triggerId, StringComparison.OrdinalIgnoreCase)));

        if (trigger is null)
        {
            return Results.NotFound($"Webhook trigger '{triggerId}' not found for workflow '{workflowId}'.");
        }

        if (trigger.Properties.TryGetValue("secret", out var secret) && !string.IsNullOrWhiteSpace(secret))
        {
            if (!httpRequest.Headers.TryGetValue("x-mullai-secret", out var provided) ||
                !string.Equals(provided.ToString(), secret, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }
        }

        var payloadJson = payload.GetRawText();
        var input = !string.IsNullOrWhiteSpace(trigger.Input)
            ? trigger.Input.Replace("{{payload}}", payloadJson, StringComparison.OrdinalIgnoreCase)
            : payloadJson;

        if (string.IsNullOrWhiteSpace(input))
        {
            return Results.BadRequest("Trigger input is required.");
        }

        var sessionKey = string.IsNullOrWhiteSpace(trigger.SessionKey)
            ? $"workflow-{workflow.Id}-{trigger.Id}"
            : trigger.SessionKey.Trim();

        var maxAttempts = Math.Max(1, runtimeOptions.Value.DefaultMaxAttempts);
        var workItem = new MullaiTaskWorkItem
        {
            TaskId = Guid.NewGuid().ToString("N"),
            SessionKey = sessionKey,
            AgentName = $"workflow:{workflow.Id}",
            Prompt = input,
            Source = MullaiTaskSource.System,
            MaxAttempts = maxAttempts,
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = workflow.Id,
                ["triggerId"] = trigger.Id,
                ["triggerType"] = trigger.Type
            }
        };

        await queue.EnqueueAsync(workItem, cancellationToken).ConfigureAwait(false);
        await statusStore.MarkQueuedAsync(workItem, cancellationToken).ConfigureAwait(false);

        return Results.Accepted($"/api/mullai/tasks/{workItem.TaskId}", new { workItem.TaskId, workItem.SessionKey });
    }
}
