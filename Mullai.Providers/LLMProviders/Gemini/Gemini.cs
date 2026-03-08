using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Global.Config.OpenTelemetry;

namespace Mullai.Providers.LLMProviders.Gemini;

public static class Gemini
{
    public static IServiceCollection AddGeminiChatClient(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        var chatClient = GetGeminiChatClient(configuration, loggerFactory, httpClient);

        services.AddSingleton<IChatClient>(chatClient);

        return services;
    }

    public static IChatClient GetGeminiChatClient(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        var apiKey = configuration["Gemini:ApiKey"];
        var modelId = configuration["Gemini:ModelId"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is missing from configuration.");
        }

        // Ideally we would pass httpClient to the Gemini Client, but Google.GenAI might not support it directly in a simple way
        // without custom HttpClient setup. For now we use the default constructor pattern from sample.
        var geminiClient = new Client(vertexAI: false, apiKey: apiKey);
        
        var chatClient = geminiClient
            .AsIChatClient(modelId)
            .AsBuilder()
            .UseOpenTelemetry(
                sourceName: OpenTelemetrySettings.ServiceName, 
                configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();

        return chatClient;
    }
}
