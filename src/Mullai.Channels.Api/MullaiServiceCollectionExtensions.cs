using Microsoft.Extensions.AI;
using Mullai.Agents;
using Mullai.Channels.Core;
using Mullai.Memory;
using Mullai.Providers.LLMProviders.OllamaOpenAI;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.WeatherTool;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Providers.LLMProviders.Gemini;
using Mullai.Providers.LLMProviders.Groq;
using Mullai.Providers.LLMProviders.Cerebras;
using Mullai.Channels.Telegram;
using Mullai.Global.ServiceConfiguration;

namespace Mullai.Channels.Api;

public static class MullaiServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureMullaiServices(configuration)
            .AddMullaiChannelsCore()
            .AddTelegramChannel(configuration);
        return services;
    }
}
