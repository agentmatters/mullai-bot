using System.Text.Json;
using Mullai.Abstractions.WorkflowState;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

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
        workflowGroup.MapGet("/runs", GetWorkflowRunsAsync);
        workflowGroup.MapGet("/runs/{taskId}/events", GetWorkflowRunEventsAsync);
        workflowGroup.MapGet("/outputs/deadletter", GetOutputDeadLetterAsync);
        workflowGroup.MapPost("/outputs/deadletter/{failureId}/replay", ReplayOutputDeadLetterAsync);
        workflowGroup.MapGet("/{workflowId}/state", GetWorkflowStateAsync);
        workflowGroup.MapGet("/{workflowId}/state/{key}", GetWorkflowStateKeyAsync);
        workflowGroup.MapPost("/{workflowId}/state/{key}", UpsertWorkflowStateAsync);
        workflowGroup.MapDelete("/{workflowId}/state/{key}", DeleteWorkflowStateAsync);

        return endpoints;
    }

    private static async Task<IResult> EnqueueTaskAsync(
        MullaiTaskSubmitRequest request,
        IMullaiTaskQueue queue,
        IMullaiTaskStatusStore statusStore,
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt)) return Results.BadRequest("Prompt is required.");

        if (string.IsNullOrWhiteSpace(request.SessionKey)) return Results.BadRequest("SessionKey is required.");

        var maxAttempts = request.MaxAttempts is > 0
            ? request.MaxAttempts.Value
            : runtimeOptions.Value.DefaultMaxAttempts;
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
        var workflow = registry.GetById(workflowId);
        if (workflow is null) return Results.NotFound($"Workflow '{workflowId}' was not found.");
        if (!workflow.IsEnabled) return Results.Conflict($"Workflow '{workflowId}' is disabled.");

        if (string.IsNullOrWhiteSpace(request.Input)) return Results.BadRequest("Input is required.");

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
        if (workflow is null) return Results.NotFound($"Workflow '{workflowId}' was not found.");
        if (!workflow.IsEnabled) return Results.Conflict($"Workflow '{workflowId}' is disabled.");

        var trigger = workflow.Triggers.FirstOrDefault(t =>
            t.Enabled &&
            t.Type.Equals("webhook", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(t.Id, triggerId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(t.Name, triggerId, StringComparison.OrdinalIgnoreCase)));

        if (trigger is null)
            return Results.NotFound($"Webhook trigger '{triggerId}' not found for workflow '{workflowId}'.");

        if (trigger.Properties.TryGetValue("secret", out var secret) && !string.IsNullOrWhiteSpace(secret))
            if (!httpRequest.Headers.TryGetValue("x-mullai-secret", out var provided) ||
                !string.Equals(provided.ToString(), secret, StringComparison.Ordinal))
                return Results.Unauthorized();

        var payloadJson = payload.GetRawText();
        var input = !string.IsNullOrWhiteSpace(trigger.Input)
            ? trigger.Input.Replace("{{payload}}", payloadJson, StringComparison.OrdinalIgnoreCase)
            : payloadJson;

        if (string.IsNullOrWhiteSpace(input)) return Results.BadRequest("Trigger input is required.");

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

    private static async Task<IResult> GetWorkflowRunsAsync(
        IMullaiTaskStatusStore statusStore,
        string? workflowId = null,
        string? state = null,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var recent = await statusStore.GetRecentAsync(Math.Max(1, take), cancellationToken).ConfigureAwait(false);
        var filtered = recent.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(workflowId))
            filtered = filtered.Where(snapshot =>
                string.Equals(snapshot.WorkflowId, workflowId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(state))
            filtered = filtered.Where(snapshot =>
                string.Equals(snapshot.State.ToString(), state.Trim(), StringComparison.OrdinalIgnoreCase));

        return Results.Ok(filtered);
    }

    private static async Task<IResult> GetWorkflowRunEventsAsync(
        string taskId,
        IWorkflowRunEventStore runEventStore,
        int take = 500,
        CancellationToken cancellationToken = default)
    {
        var results = await runEventStore
            .GetForTaskAsync(taskId, Math.Max(1, take), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetOutputDeadLetterAsync(
        IWorkflowOutputFailureStore failureStore,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await failureStore.GetRecentAsync(Math.Max(1, take), cancellationToken).ConfigureAwait(false);
        return Results.Ok(results);
    }

    private static async Task<IResult> ReplayOutputDeadLetterAsync(
        string failureId,
        IWorkflowOutputFailureStore failureStore,
        IEnumerable<IWorkflowOutputHandler> handlers,
        IWorkflowRegistry registry,
        CancellationToken cancellationToken)
    {
        var failure = await failureStore.GetAsync(failureId, cancellationToken).ConfigureAwait(false);
        if (failure is null) return Results.NotFound();

        var definition = registry.GetById(failure.WorkflowId);
        if (definition is null) return Results.NotFound($"Workflow '{failure.WorkflowId}' was not found.");

        var handler = handlers.FirstOrDefault(h =>
            string.Equals(h.Type, failure.OutputType, StringComparison.OrdinalIgnoreCase));
        if (handler is null) return Results.NotFound($"No output handler for '{failure.OutputType}'.");

        var output = new WorkflowOutputDefinition
        {
            Type = failure.OutputType,
            Target = failure.OutputTarget,
            Properties = new Dictionary<string, string>(failure.OutputProperties)
        };

        var context = new WorkflowOutputContext
        {
            Definition = definition,
            Response = failure.Response,
            TaskId = failure.TaskId,
            SessionKey = failure.SessionKey,
            Metadata = failure.Metadata
        };

        await handler.HandleAsync(context, output, cancellationToken).ConfigureAwait(false);
        await failureStore.RemoveAsync(failure.Id, cancellationToken).ConfigureAwait(false);

        return Results.Ok();
    }

    private static async Task<IResult> GetWorkflowStateAsync(
        string workflowId,
        IWorkflowStateStore stateStore,
        CancellationToken cancellationToken)
    {
        var results = await stateStore.GetAllAsync(workflowId, cancellationToken).ConfigureAwait(false);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetWorkflowStateKeyAsync(
        string workflowId,
        string key,
        IWorkflowStateStore stateStore,
        CancellationToken cancellationToken)
    {
        var record = await stateStore.GetAsync(workflowId, key, cancellationToken).ConfigureAwait(false);
        return record is null ? Results.NotFound() : Results.Ok(record);
    }

    private static async Task<IResult> UpsertWorkflowStateAsync(
        string workflowId,
        string key,
        JsonElement payload,
        IWorkflowStateStore stateStore,
        CancellationToken cancellationToken)
    {
        var json = payload.GetRawText();
        await stateStore.UpsertAsync(workflowId, key, json, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteWorkflowStateAsync(
        string workflowId,
        string key,
        IWorkflowStateStore stateStore,
        CancellationToken cancellationToken)
    {
        await stateStore.RemoveAsync(workflowId, key, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}