using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.HtmlToMarkdownTool;

/// <summary>
///     A tool for fetching HTML content and converting it to Markdown.
/// </summary>
[Description("A tool for converting HTML content or web pages into clean, readable Markdown format.")]
public class HtmlToMarkdownTool(HtmlToMarkdownProvider provider)
{
    /// <summary>
    ///     Fetches a web page and converts it to Markdown.
    /// </summary>
    [Description("Fetches the content of a web page from a URL and returns it as Markdown.")]
    public async Task<string> FetchAsMarkdownAsync(
        [Description("The URL of the web page to fetch and convert.")]
        string url)
    {
        return await provider.FetchAndConvertAsync(url);
    }

    /// <summary>
    ///     Converts raw HTML string to Markdown.
    /// </summary>
    [Description("Converts a given raw HTML string into Markdown format.")]
    public string ConvertHtmlToMarkdown(
        [Description("The raw HTML string to convert.")]
        string html)
    {
        return provider.Convert(html);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(FetchAsMarkdownAsync);
        yield return AIFunctionFactory.Create(ConvertHtmlToMarkdown);
    }
}