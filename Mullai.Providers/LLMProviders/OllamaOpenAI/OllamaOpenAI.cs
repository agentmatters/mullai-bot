using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Global.Config.OpenTelemetry;
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
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        const string endpoint = "http://localhost:11434/v1";
        var modelId = configuration["OllamaOpenAI:ModelId"];
        var apiKey = configuration["OllamaOpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("OllamaOpenAI:ModelId is missing from configuration.");
        }
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OllamaOpenAI:ApiKey is missing from configuration.");
        }

        OpenAIClient openAIClient;

        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri(endpoint),
            Transport = new HttpClientPipelineTransport(httpClient)
        };

        openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), openAIOptions);
        
        var chatClient = openAIClient.GetChatClient(modelId)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(
                sourceName: OpenTelemetrySettings.ServiceName, 
                configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
        
        return chatClient;
    }
}