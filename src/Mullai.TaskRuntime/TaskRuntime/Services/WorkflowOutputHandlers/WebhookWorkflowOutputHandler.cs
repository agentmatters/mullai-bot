using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;

namespace Mullai.TaskRuntime.Services.WorkflowOutputHandlers;

public sealed class WebhookWorkflowOutputHandler : IWorkflowOutputHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookWorkflowOutputHandler> _logger;

    public WebhookWorkflowOutputHandler(HttpClient httpClient, ILogger<WebhookWorkflowOutputHandler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => "webhook";

    public async Task HandleAsync(WorkflowOutputContext context, WorkflowOutputDefinition output, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(output.Target))
        {
            _logger.LogWarning("Webhook output missing target URL for workflow {WorkflowId}.", context.Definition.Id);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, output.Target);
        foreach (var header in output.Properties.Where(p => p.Key.StartsWith("header:", StringComparison.OrdinalIgnoreCase)))
        {
            var name = header.Key["header:".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                request.Headers.TryAddWithoutValidation(name, header.Value);
            }
        }

        var payload = new
        {
            workflowId = context.Definition.Id,
            taskId = context.TaskId,
            sessionKey = context.SessionKey,
            response = context.Response,
            metadata = context.Metadata
        };

        request.Content = JsonContent.Create(payload);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook output for workflow {WorkflowId} returned status {StatusCode}.",
                    context.Definition.Id,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook output failed for workflow {WorkflowId}.", context.Definition.Id);
        }
    }
}
