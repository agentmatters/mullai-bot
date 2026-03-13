using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.OpenTelemetry.OpenTelemetry;
using OpenAI;


namespace Mullai.Providers.LLMProviders.OpenRouter;

public static class OpenRouter
{
    public static IServiceCollection AddOpenRouterChatClient(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
        )
    {
        var chatClient = GetOpenRouterChatClient(configuration, loggerFactory, httpClient);

        services.AddSingleton<IChatClient>(chatClient);

        return services;
    }

    public static IChatClient GetOpenRouterChatClient(
        IConfiguration configuration,
        HttpClient httpClient,
        string? modelId = null
    )
    {
        return OpenAICompatibleProvider.CreateChatClient(
            "OpenRouter",
            "https://openrouter.ai/api/v1",
            configuration,
            httpClient,
            modelIdOverride: modelId);
    }

    [Obsolete("Use GetOpenRouterChatClient(configuration, httpClient, modelId) instead.")]
    public static IChatClient GetOpenRouterChatClient(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    ) => GetOpenRouterChatClient(configuration, httpClient);
}