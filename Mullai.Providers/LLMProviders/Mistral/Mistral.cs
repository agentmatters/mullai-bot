using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Global.Config.OpenTelemetry;
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
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        const string endpoint = "https://api.mistral.ai/v1";
        var modelId = configuration["Mistral:ModelId"];
        var apiKey = configuration["Mistral:ApiKey"];

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Mistral:ModelId is missing from configuration.");
        }
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Mistral:ApiKey is missing from configuration.");
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
            .Use((inner, services) => new MistralChatMessageInterceptor(inner))
            .UseOpenTelemetry(
                sourceName: OpenTelemetrySettings.ServiceName, 
                configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
        
        return chatClient;
    }
}
