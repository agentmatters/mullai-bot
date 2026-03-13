using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.CliTool;

public static class CliToolExtension
{
    public static IServiceCollection AddCliTool(
        this IServiceCollection services)
    {
        services.AddSingleton<CliProvider>();
        services.AddSingleton<CliTool>();

        return services;
    }
}
