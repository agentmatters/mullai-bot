using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Mullai.Providers.LLMProviders.OpenAI;

public static class OpenAI
{
    public static IServiceCollection AddOpenAIChatClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var modelId = configuration["OpenAI:ModelId"] ?? "gpt-4o";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is missing from configuration.");

        var openAIClient = new OpenAIClient(apiKey);

        var chatClient = openAIClient.GetChatClient(modelId).AsIChatClient();

        services.AddSingleton<IChatClient>(chatClient);

        return services;
    }
}