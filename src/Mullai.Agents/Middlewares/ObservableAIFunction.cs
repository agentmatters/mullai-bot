using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Agents.Middlewares;

/// <summary>
///     A DelegatingAIFunction wrapper that routes function invocations through
///     the agent's function invocation middleware pipeline (e.g. FunctionCallingMiddleware).
///     This replicates the SDK's internal MiddlewareEnabledFunction pattern for
///     dynamically loaded tools that weren't wrapped at agent construction time.
/// </summary>
internal sealed class ObservableAIFunction : DelegatingAIFunction
{
    private readonly AIAgent _agent;

    private readonly
        Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>,
            CancellationToken, ValueTask<object?>> _middlewareCallback;

    public ObservableAIFunction(
        AIAgent agent,
        AIFunction innerFunction,
        Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>,
            CancellationToken, ValueTask<object?>> middlewareCallback)
        : base(innerFunction)
    {
        _agent = agent;
        _middlewareCallback = middlewareCallback;
    }

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // Use the ambient FunctionInvocationContext set by FunctionInvokingChatClient,
        // or create a fallback if invoked outside that pipeline.
        var context = FunctionInvokingChatClient.CurrentContext
                      ?? new FunctionInvocationContext
                      {
                          Arguments = arguments,
                          Function = InnerFunction,
                          CallContent = new FunctionCallContent(
                              string.Empty,
                              InnerFunction.Name,
                              new Dictionary<string, object?>(arguments))
                      };

        return await _middlewareCallback(_agent, context, CoreLogicAsync, cancellationToken)
            .ConfigureAwait(false);

        ValueTask<object?> CoreLogicAsync(FunctionInvocationContext ctx, CancellationToken ct)
        {
            return base.InvokeCoreAsync(ctx.Arguments, ct);
        }
    }
}