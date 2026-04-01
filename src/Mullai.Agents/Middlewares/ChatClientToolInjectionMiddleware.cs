using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Agents.Middlewares;

/// <summary>
///     An IChatClient-level middleware that merges newly loaded session tools into
///     ChatOptions.Tools on each LLM call.
///     This runs within FunctionInvokingChatClient's internal tool-calling loop,
///     ensuring that tools loaded mid-run (via DynamicToolLoader) are immediately
///     visible to the LLM on the next iteration.
///     New tools are wrapped as <see cref="ObservableAIFunction" /> so that the
///     FunctionCallingMiddleware callback fires for them even in the loading turn.
/// </summary>
public class ChatClientToolInjectionMiddleware : DelegatingChatClient
{
    private readonly
        Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>,
            CancellationToken, ValueTask<object?>> _middlewareCallback;

    private readonly IList<AITool> _sessionTools;

    /// <summary>
    ///     The agent reference, set after construction to break the circular dependency
    ///     (middleware wraps IChatClient -> IChatClient is needed to build agent -> agent is needed by middleware).
    /// </summary>
    private AIAgent? _agent;

    public ChatClientToolInjectionMiddleware(
        IChatClient innerClient,
        IList<AITool> sessionTools,
        Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>,
            CancellationToken, ValueTask<object?>> middlewareCallback)
        : base(innerClient)
    {
        _sessionTools = sessionTools ?? throw new ArgumentNullException(nameof(sessionTools));
        _middlewareCallback = middlewareCallback ?? throw new ArgumentNullException(nameof(middlewareCallback));
    }

    /// <summary>
    ///     Sets the agent reference after the agent has been built.
    ///     Must be called before the middleware is used.
    /// </summary>
    public void SetAgent(AIAgent agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options != null) MergeNewTools(options);

        return await base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (options != null) MergeNewTools(options);

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            yield return update;
    }

    /// <summary>
    ///     Appends any session tools not already present in options.Tools,
    ///     wrapping them as <see cref="ObservableAIFunction" /> for middleware callback support.
    /// </summary>
    private void MergeNewTools(ChatOptions options)
    {
        options.Tools ??= new List<AITool>();

        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in options.Tools)
            if (tool is AIFunction func)
                existingNames.Add(func.Name);

        foreach (var sessionTool in _sessionTools)
            if (sessionTool is AIFunction func && !existingNames.Contains(func.Name))
            {
                var toolToAdd = _agent != null
                    ? new ObservableAIFunction(_agent, func, _middlewareCallback)
                    : sessionTool; // Fallback to raw if agent not yet set (shouldn't happen in practice)

                options.Tools.Add(toolToAdd);
                existingNames.Add(func.Name);
            }
    }
}