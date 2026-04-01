using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.WebTool;

/// <summary>
///     A tool for web searching and URL fetching.
/// </summary>
[Description("A tool for searching the web and fetching content from URLs.")]
public class WebTool(WebProvider webProvider)
{
    /// <summary>
    ///     Searches the web for a query.
    /// </summary>
    [Description("Searches the web for the given query and returns summarized results.")]
    public async Task<string> SearchWeb(
        [Description("The search query.")] string query,
        [Description("The number of results to return (default: 8).")]
        int numResults = 8)
    {
        return await webProvider.SearchWebAsync(query, numResults);
    }

    /// <summary>
    ///     Fetches content from a specified URL.
    /// </summary>
    [Description("Fetches the raw content from a given URL.")]
    public async Task<string> FetchUrl(
        [Description("The URL to fetch content from.")]
        string url)
    {
        return await webProvider.FetchUrlAsync(url);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(SearchWeb);
        yield return AIFunctionFactory.Create(FetchUrl);
    }
}