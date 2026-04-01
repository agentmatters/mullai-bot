using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Mullai.Providers.LLMProviders.Nvidia;

public static class Nvidia
{
    public static IChatClient GetNvidiaChatClient(
        IConfiguration configuration,
        HttpClient httpClient,
        string? modelId = null
    )
    {
        return OpenAICompatibleProvider.CreateChatClient(
            "Nvidia",
            "https://integrate.api.nvidia.com/v1",
            configuration,
            httpClient,
            modelIdOverride: modelId);
    }
}