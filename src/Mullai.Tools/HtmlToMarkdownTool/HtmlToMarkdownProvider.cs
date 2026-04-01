using ReverseMarkdown;

namespace Mullai.Tools.HtmlToMarkdownTool;

public class HtmlToMarkdownProvider(HttpClient httpClient)
{
    private readonly Converter _converter = new();

    public async Task<string> FetchAndConvertAsync(string url)
    {
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return "Error: URL must start with http:// or https://";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return $"Error: Failed to fetch URL. Status: {response.StatusCode}";

            var html = await response.Content.ReadAsStringAsync();

            return _converter.Convert(html);
        }
        catch (Exception ex)
        {
            return $"Error during HTML to Markdown conversion: {ex.Message}";
        }
    }

    public string Convert(string html)
    {
        try
        {
            return _converter.Convert(html);
        }
        catch (Exception ex)
        {
            return $"Error during HTML to Markdown conversion: {ex.Message}";
        }
    }
}