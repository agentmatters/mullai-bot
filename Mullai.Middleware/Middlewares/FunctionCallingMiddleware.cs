using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mullai.Middleware.Middlewares;

public class FunctionCallingMiddleware
{
    private readonly ILogger<FunctionCallingMiddleware> _logger;

    public FunctionCallingMiddleware(ILogger<FunctionCallingMiddleware> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        AIAgent agent, 
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent Id: {AgentId}", agent.Id);
        
        // Format the list of dictionaries into a readable string
        var formattedArguments = string.Join("\n", context.Arguments
            .Select(kvp => $"  {kvp.Key}: {kvp.Value ?? "null"}"));

        _logger.LogInformation("Invoking Function: {FunctionName} \nArguments:\n{Arguments}", 
            context.Function.Name, 
            formattedArguments);
        
        var result = await next(context, cancellationToken);
        
        return result;
    }
}
