using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Channels.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiChannelsCore(this IServiceCollection services)
    {
        services.AddSingleton<ChannelManager>();
        
        // At runtime, specific channel adapters (e.g. Telegram) will be added to the DI container.
        // E.g., services.AddSingleton<IChannelAdapter, TelegramAdapter>();
        
        return services;
    }
}
