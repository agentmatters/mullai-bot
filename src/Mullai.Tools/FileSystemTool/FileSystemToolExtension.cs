using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.FileSystemTool;

public static class FileSystemToolExtension
{
    public static IServiceCollection AddFileSystemTool(
        this IServiceCollection services)
    {
        services.AddSingleton<FileSystemProvider>();
        services.AddSingleton<FileSystemTool>();

        return services;
    }
}
