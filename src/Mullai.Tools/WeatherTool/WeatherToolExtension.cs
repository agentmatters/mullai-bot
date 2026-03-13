using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.WeatherTool;

public static class WeatherToolExtension
{
    public static IServiceCollection AddWeatherTool(
        this IServiceCollection services)
    {
        services.AddSingleton<GeolocationProvider>();
        services.AddSingleton<WeatherProvider>();
        services.AddSingleton<CurrentTimeProvider>();
        services.AddSingleton<WeatherTool>();

        return services;
    }
}