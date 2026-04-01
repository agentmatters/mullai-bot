using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Agents.Middlewares;

/// <summary>
///     An agent-pipeline middleware that injects the latest session tools into
///     the <see cref="AgentRunOptions" /> before each run.
///     This MUST sit ABOVE (outermost to) the FunctionInvocationDelegatingAgent
///     in the agent pipeline so that the SDK's ConfigureOptions middleware wraps
///     ALL tools (including dynamically loaded ones) as MiddlewareEnabledFunction,
///     which is required for the FunctionCallingMiddleware callback to fire.
/// </summary>
public static class ToolCallDynamicInjectionMiddleware
{
    /// <summary>
    ///     Creates an agent-pipeline middleware delegate that injects <paramref name="sessionTools" />
    ///     into the run options before delegating to the inner agent.
    /// </summary>
    public static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?,
            Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken, Task>, CancellationToken,
            Task>
        Create(IList<AITool> sessionTools)
    {
        return async (messages, session, options, next, cancellationToken) =>
        {
            // Ensure we have ChatClientAgentRunOptions with the latest session tools
            if (options is ChatClientAgentRunOptions ccOptions)
            {
                ccOptions.ChatOptions ??= new ChatOptions();
                ccOptions.ChatOptions.Tools = sessionTools.ToList();
            }
            else
            {
                options = new ChatClientAgentRunOptions
                {
                    ChatOptions = new ChatOptions { Tools = sessionTools.ToList() },
                    ResponseFormat = options?.ResponseFormat,
                    AllowBackgroundResponses = options?.AllowBackgroundResponses,
                    AdditionalProperties = options?.AdditionalProperties
                };
            }

            await next(messages, session, options, cancellationToken);
        };
    }
}