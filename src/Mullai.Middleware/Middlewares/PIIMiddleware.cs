using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mullai.Middleware.Middlewares;

public class PIIMiddleware
{
    private readonly ILogger<PIIMiddleware> _logger;

    public PIIMiddleware(ILogger<PIIMiddleware> logger)
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
        // Redact PII information from input messages
        var filteredMessages = FilterMessages(messages);
        _logger.LogInformation("Pii Middleware - Filtered Messages Pre-Run");

        var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken)
            .ConfigureAwait(false);

        // Redact PII information from output messages
        response.Messages = FilterMessages(response.Messages);

        _logger.LogInformation("Pii Middleware - Filtered Messages Post-Run");

        return response;
    }

    private static IList<ChatMessage> FilterMessages(IEnumerable<ChatMessage> messages)
    {
        return messages.Select(m => new ChatMessage(m.Role, FilterPii(m.Text))).ToList();
    }

    private static string FilterPii(string? content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        // Regex patterns for PII detection (simplified for demonstration)
        Regex[] piiPatterns =
        [
            new(@"\b\d{3}-\d{3}-\d{4}\b", RegexOptions.Compiled), // Phone number (e.g., 123-456-7890)
            new(@"\b[\w\.-]+@[\w\.-]+\.\w+\b", RegexOptions.Compiled), // Email address
            new(@"\b[A-Z][a-z]+\s[A-Z][a-z]+\b", RegexOptions.Compiled) // Full name (e.g., John Doe)
        ];

        foreach (var pattern in piiPatterns) content = pattern.Replace(content, "[REDACTED: PII]");

        return content;
    }
}