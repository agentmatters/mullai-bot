using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.CodeSearchTool;

/// <summary>
///     A tool for searching for code contexts, APIs, and documentation.
/// </summary>
[Description("A tool for searching for relevant context for APIs, Libraries, and SDKs. Powered by Exa.")]
public class CodeSearchTool(CodeSearchProvider codeSearchProvider)
{
    /// <summary>
    ///     Searches for code snippets and documentation.
    /// </summary>
    [Description(
        "Search query to find relevant context for APIs, Libraries, and SDKs. For example, 'React useState hook examples', 'Python pandas dataframe filtering', 'Express.js middleware', 'Next js partial prerendering configuration'.")]
    public async Task<string> SearchCode(
        [Description("The search query.")] string query,
        [Description("The number of tokens to return (1000-50000). Default is 5000.")]
        int tokensNum = 5000)
    {
        return await codeSearchProvider.SearchCodeAsync(query, tokensNum);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(SearchCode);
    }
}