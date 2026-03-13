using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Channels.Core.Abstractions;
using Telegram.Bot;

namespace Mullai.Channels.Telegram;

public static class TelegramServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        
        // Retrieve token immediately for bot client setup
        var options = configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>();
        var botToken = options?.BotToken ?? string.Empty;

        // Register TelegramBotClient
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    var tokenOptions = new TelegramBotClientOptions(botToken);
                    return new TelegramBotClient(tokenOptions, httpClient);
                });

        // Register TelegramChannelAdapter as a Singleton and a HostedService
        services.AddSingleton<TelegramChannelAdapter>();
        services.AddSingleton<IChannelAdapter>(sp => sp.GetRequiredService<TelegramChannelAdapter>());
        services.AddHostedService(sp => sp.GetRequiredService<TelegramChannelAdapter>());

        return services;
    }
}
