using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Mullai.Providers.LLMProviders.OpenRouter;

public static class OpenRouter
{
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
}