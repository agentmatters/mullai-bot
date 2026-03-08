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
using Mullai.Channels.Telegram;

namespace Mullai.Channels.Api;

public static class MullaiServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton<HttpClient>()
            .AddSingleton<IChatClient>(sp => 
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var httpClient = sp.GetRequiredService<HttpClient>();
    
                return Gemini.GetGeminiChatClient(configuration, loggerFactory, httpClient);
            })
            .AddWeatherTool()
            .AddCliTool()
            .AddFileSystemTool()
            .AddUserMemory()
            .AddMullaiSkills()
            .AddSingleton<AgentFactory>()
            .AddMullaiChannelsCore()
            .AddTelegramChannel(configuration);

        return services;
    }
}
