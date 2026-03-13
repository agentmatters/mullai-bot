using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Mullai.Middleware.Middlewares;

public class RateLimitMiddleware
{
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly ResiliencePipeline<AgentResponse> _pipeline;

    public RateLimitMiddleware(ILogger<RateLimitMiddleware> logger)
    {
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder<AgentResponse>()
            .AddRetry(new RetryStrategyOptions<AgentResponse>
            {
                ShouldHandle = new PredicateBuilder<AgentResponse>()
                    .Handle<Exception>(IsRateLimitException),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(4),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args => 
                {
                    _logger.LogWarning(args.Outcome.Exception, "Rate limit hit. Retrying in {Delay}. Attempt {AttemptNumber}", args.RetryDelay, args.AttemptNumber);
                    return default;
                }
            })
            .Build();
    }

    public async Task<AgentResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages, 
        AgentSession? session, 
        AgentRunOptions? options, 
        AIAgent innerAgent, 
        CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(async ct => 
        {
            return await innerAgent.RunAsync(messages, session, options, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsRateLimitException(Exception ex)
    {
        // Many providers use HTTP 429 Too Many Requests for rate limits.
        // It might be an HttpRequestException or an HttpOperationException or AIException.
        // Usually, 429 status code or message containing "429" or "rate limit" or "TooManyRequests"
        if (ex.Message.Contains("429") || 
            ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || 
            ex.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var exceptionType = ex.GetType().Name;
        if (exceptionType.Contains("RateLimit", StringComparison.OrdinalIgnoreCase) || 
            exceptionType.Contains("ClientResultException", StringComparison.OrdinalIgnoreCase)) 
        {
             if (ex.Message.Contains("429")) return true;
        }

        return false;
    }
}
