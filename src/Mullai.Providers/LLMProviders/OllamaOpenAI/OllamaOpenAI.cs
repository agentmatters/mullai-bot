using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.OpenTelemetry.OpenTelemetry;
using OpenAI;

namespace Mullai.Providers.LLMProviders.OllamaOpenAI;

public static class OllamaOpenAI
{
    public static IServiceCollection AddOllamaOpenAIChatClient(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        var chatClient = GetOllamaOpenAIChatClient(configuration, loggerFactory, httpClient);

        services.AddSingleton<IChatClient>(chatClient);

        return services;
    }

    public static IChatClient GetOllamaOpenAIChatClient(
        IConfiguration configuration,
        HttpClient httpClient,
        string? modelId = null
    )
    {
        return OpenAICompatibleProvider.CreateChatClient(
            "OllamaOpenAI",
            "http://localhost:11434/v1",
            configuration,
            httpClient,
            modelIdOverride: modelId);
    }

    [Obsolete("Use GetOllamaOpenAIChatClient(configuration, httpClient, modelId) instead.")]
    public static IChatClient GetOllamaOpenAIChatClient(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    ) => GetOllamaOpenAIChatClient(configuration, httpClient);}