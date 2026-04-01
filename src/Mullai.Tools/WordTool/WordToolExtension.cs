using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.WordTool;

public static class WordToolExtension
{
    public static IServiceCollection AddWordTool(
        this IServiceCollection services)
    {
        services.AddSingleton<WordProvider>();
        services.AddSingleton<WordTool>();

        return services;
    }
}