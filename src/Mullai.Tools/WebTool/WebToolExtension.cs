using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.WebTool;

public static class WebToolExtension
{
    public static IServiceCollection AddWebTool(
        this IServiceCollection services)
    {
        services.AddHttpClient<WebProvider>();
        services.AddSingleton<WebTool>();

        return services;
    }
}