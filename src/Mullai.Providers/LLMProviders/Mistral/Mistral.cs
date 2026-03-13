using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.OpenTelemetry.OpenTelemetry;
using OpenAI;


namespace Mullai.Providers.LLMProviders.Mistral;

public static class Mistral
{
    public static IServiceCollection AddMistralChatClient(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
        )
    {
        var chatClient = GetMistralChatClient(configuration, loggerFactory, httpClient);

        services.AddSingleton<IChatClient>(chatClient);
        return services;
    }

    public static IChatClient GetMistralChatClient(
        IConfiguration configuration,
        HttpClient httpClient,
        string? modelId = null
    )
    {
        return OpenAICompatibleProvider.CreateChatClient(
            "Mistral",
            "https://api.mistral.ai/v1",
            configuration,
            httpClient,
            builder => builder.Use((inner, services) => new MistralChatMessageInterceptor(inner)),
            modelIdOverride: modelId);
    }

    [Obsolete("Use GetMistralChatClient(configuration, httpClient, modelId) instead.")]
    public static IChatClient GetMistralChatClient(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    ) => GetMistralChatClient(configuration, httpClient);
}
