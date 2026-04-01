using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mullai.Middleware.Middlewares;

public class GuardrailMiddleware
{
    private readonly ILogger<GuardrailMiddleware> _logger;

    public GuardrailMiddleware(ILogger<GuardrailMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<AgentResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        // Redact keywords from input messages
        var filteredMessages = FilterMessages(messages);

        _logger.LogInformation("Guardrail Middleware - Filtered messages Pre-Run");

        // Proceed with the agent run
        var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken);

        // Redact keywords from output messages
        response.Messages = FilterMessages(response.Messages);

        _logger.LogInformation("Guardrail Middleware - Filtered messages Post-Run");

        return response;
    }

    private static List<ChatMessage> FilterMessages(IEnumerable<ChatMessage> messages)
    {
        return messages.Select(m => new ChatMessage(m.Role, FilterContent(m.Text))).ToList();
    }

    private static string FilterContent(string? content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        foreach (var keyword in new[] { "harmful", "illegal", "violence" })
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return "[REDACTED: Forbidden content]";

        return content;
    }
}