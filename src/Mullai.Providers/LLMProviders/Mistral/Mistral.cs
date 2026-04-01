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
        var apiKey = configuration["Mistral:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Mistral:ApiKey is missing from configuration.");

        var endpoint = configuration["Mistral:Endpoint"] ?? "https://api.mistral.ai/v1/chat/completions";
        
        var client = new MistralChatClient(httpClient, new Uri(endpoint))
        {
            OnBeforeRequest = req => req.Headers.Add("Authorization", $"Bearer {apiKey}")
        };

        return new MistralChatMessageInterceptor(client);
    }

    [Obsolete("Use GetMistralChatClient(configuration, httpClient, modelId) instead.")]
    public static IChatClient GetMistralChatClient(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    ) => GetMistralChatClient(configuration, httpClient);
}
