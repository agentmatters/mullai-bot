using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.RestApiTool;

public static class RestApiToolExtension
{
    public static IServiceCollection AddRestApiTool(
        this IServiceCollection services)
    {
        services.AddSingleton<RestApiProvider>();
        services.AddSingleton<RestApiTool>();

        return services;
    }
}