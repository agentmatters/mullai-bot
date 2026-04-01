using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.CodeSearchTool;

public static class CodeSearchToolExtension
{
    public static IServiceCollection AddCodeSearchTool(
        this IServiceCollection services)
    {
        services.AddHttpClient<CodeSearchProvider>();
        services.AddSingleton<CodeSearchTool>();

        return services;
    }
}