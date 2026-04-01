using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mullai.Abstractions.Observability;

namespace Mullai.Middleware.Middlewares;

/// <summary>
///     Middleware that intercepts every tool invocation and:
///     1. Logs it (existing behaviour)
///     2. Publishes a <see cref="ToolCallObservation" /> via an optional callback
///     so the UI layer can display live tool call info without coupling to the middleware.
/// </summary>
public class FunctionCallingMiddleware
{
    private readonly ILogger<FunctionCallingMiddleware> _logger;

    public FunctionCallingMiddleware(ILogger<FunctionCallingMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Optional observer callback. Inject from the TUI bootstrap layer
    ///     to receive real-time tool call events without creating a dependency
    ///     from the middleware onto the UI.
    /// </summary>
    public Action<ToolCallObservation>? OnToolCallObserved { get; set; }

    public async ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent Id: {AgentId}", agent.Id);

        var formattedArguments = string.Join("\n", context.Arguments
            .Select(kvp => $"  {kvp.Key}: {kvp.Value ?? "null"}"));

        _logger.LogInformation("Invoking Function: {FunctionName} \nArguments:\n{Arguments}",
            context.Function.Name,
            formattedArguments);

        var startedAt = DateTimeOffset.UtcNow;
        var callId = Guid.NewGuid().ToString("N");
        object? result = null;
        string? error = null;
        var succeeded = false;

        // Emit 'started' observation
        var startObservation = new ToolCallObservation(
            callId,
            context.Function.Name,
            new Dictionary<string, object?>(context.Arguments),
            false,
            null,
            null,
            startedAt,
            null);
        OnToolCallObserved?.Invoke(startObservation);

        try
        {
            result = await next(context, cancellationToken);
            succeeded = true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "Function {FunctionName} threw an exception", context.Function.Name);
            throw;
        }
        finally
        {
            var finalObservation = new ToolCallObservation(
                callId,
                context.Function.Name,
                new Dictionary<string, object?>(context.Arguments),
                succeeded,
                result?.ToString(),
                error,
                startedAt,
                DateTimeOffset.UtcNow);

            OnToolCallObserved?.Invoke(finalObservation);
        }

        return result;
    }
}