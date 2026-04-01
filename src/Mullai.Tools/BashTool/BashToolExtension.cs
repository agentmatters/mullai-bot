using Microsoft.Extensions.DependencyInjection;
using Mullai.Tools.CliTool;

namespace Mullai.Tools.BashTool;

public static class BashToolExtension
{
    public static IServiceCollection AddBashTool(
        this IServiceCollection services)
    {
        services.AddSingleton<CliProvider>();
        services.AddSingleton<BashTool>();

        return services;
    }
}