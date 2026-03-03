using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        const string endpoint = "https://openrouter.ai/api/v1";
        var modelId = configuration["OpenRouter:ModelId"];
        var apiKey = configuration["OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("OpenRouter:ModelId is missing from configuration.");
        }
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenRouter:ApiKey is missing from configuration.");
        }

        OpenAIClient openAIClient;

        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri(endpoint),
            Transport = new HttpClientPipelineTransport(httpClient)
        };

        openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), openAIOptions);
        
        var chatClient = openAIClient.GetChatClient(modelId).AsIChatClient();
        
        return chatClient;
    }
}