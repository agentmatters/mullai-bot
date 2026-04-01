using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.HtmlToMarkdownTool;

public static class HtmlToMarkdownToolExtension
{
    public static IServiceCollection AddHtmlToMarkdownTool(
        this IServiceCollection services)
    {
        services.AddSingleton<HtmlToMarkdownProvider>();
        services.AddSingleton<HtmlToMarkdownTool>();

        return services;
    }
}