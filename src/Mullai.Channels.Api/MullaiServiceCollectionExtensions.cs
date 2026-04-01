using Mullai.Channels.Core;
using Mullai.Channels.Telegram;
using Mullai.Global.ServiceConfiguration;

namespace Mullai.Channels.Api;

public static class MullaiServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiAgentServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .ConfigureMullaiServices(configuration)
            .AddMullaiChannelsCore()
            .AddTelegramChannel(configuration);
        return services;
    }
}