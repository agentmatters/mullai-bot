using System.Net.Http.Json;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.WorkflowOutputHandlers;

public sealed class WebhookWorkflowOutputHandler : IWorkflowOutputHandler
{
    private readonly IWorkflowOutputFailureStore _failureStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookWorkflowOutputHandler> _logger;

    public WebhookWorkflowOutputHandler(
        HttpClient httpClient,
        IWorkflowOutputFailureStore failureStore,
        ILogger<WebhookWorkflowOutputHandler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _failureStore = failureStore ?? throw new ArgumentNullException(nameof(failureStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => "webhook";

    public async Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(output.Target))
        {
            _logger.LogWarning("Webhook output missing target URL for workflow {WorkflowId}.", context.Definition.Id);
            return;
        }

        var payload = new
        {
            workflowId = context.Definition.Id,
            taskId = context.TaskId,
            sessionKey = context.SessionKey,
            response = context.Response,
            metadata = context.Metadata
        };

        var retries = GetIntProperty(output, "retries", 3);
        var delaySeconds = Math.Max(1, GetIntProperty(output, "retryDelaySeconds", 5));

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, output.Target)
                {
                    Content = JsonContent.Create(payload)
                };
                foreach (var header in output.Properties.Where(p =>
                             p.Key.StartsWith("header:", StringComparison.OrdinalIgnoreCase)))
                {
                    var name = header.Key["header:".Length..].Trim();
                    if (!string.IsNullOrWhiteSpace(name)) request.Headers.TryAddWithoutValidation(name, header.Value);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return;

                _logger.LogWarning(
                    "Webhook output for workflow {WorkflowId} returned status {StatusCode} (attempt {Attempt}/{Retries}).",
                    context.Definition.Id,
                    response.StatusCode,
                    attempt,
                    retries);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Webhook output failed for workflow {WorkflowId} (attempt {Attempt}/{Retries}).",
                    context.Definition.Id,
                    attempt,
                    retries);
            }

            if (attempt < retries)
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
        }

        await _failureStore.AddAsync(new WorkflowOutputFailure
        {
            WorkflowId = context.Definition.Id,
            OutputType = Type,
            OutputTarget = output.Target,
            OutputProperties = new Dictionary<string, string>(output.Properties),
            TaskId = context.TaskId,
            SessionKey = context.SessionKey,
            Response = context.Response,
            Metadata = context.Metadata,
            Error = "Webhook output failed after retries.",
            Attempts = retries
        }, cancellationToken).ConfigureAwait(false);
    }

    private static int GetIntProperty(WorkflowOutputDefinition output, string key, int defaultValue)
    {
        if (!output.Properties.TryGetValue(key, out var raw)) return defaultValue;

        return int.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }
}