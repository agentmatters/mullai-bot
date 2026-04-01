using System.Net.Http.Json;
using System.Text.Json;

namespace Mullai.Tools.WebTool;

public class WebProvider(HttpClient httpClient)
{
    private const string ExaBaseUrl = "https://mcp.exa.ai/mcp";

    public async Task<string> SearchWebAsync(string query, int numResults = 8)
    {
        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = "web_search_exa",
                    arguments = new
                    {
                        query,
                        numResults,
                        type = "auto"
                    }
                }
            };

            var requestContent = JsonContent.Create(request);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, ExaBaseUrl)
            {
                Content = requestContent
            };
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.ParseAdd("application/json");
            requestMessage.Headers.Accept.ParseAdd("text/event-stream");

            var response = await httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return $"Error: Web search failed ({response.StatusCode}): {errorText}";
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var lines = responseText.Split('\n');
            foreach (var line in lines)
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.Array &&
                        content.GetArrayLength() > 0)
                        return content[0].GetProperty("text").GetString() ?? "No content found.";
                }

            return "No search results found.";
        }
        catch (Exception ex)
        {
            return $"Failed to perform web search. Error: {ex.Message}";
        }
    }

    public async Task<string> FetchUrlAsync(string url)
    {
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return "Error: URL must start with http:// or https://";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return $"Error: Failed to fetch URL. Status: {response.StatusCode}";

            var content = await response.Content.ReadAsStringAsync();
            // Basic text extraction if it's HTML (very naive)
            if (response.Content.Headers.ContentType?.MediaType == "text/html")
                // In a real scenario, we'd use a library. For now, we return the raw HTML or a simplified version.
                return content.Length > 10000 ? content.Substring(0, 10000) + "\n... (truncated)" : content;

            return content;
        }
        catch (Exception ex)
        {
            return $"Failed to fetch URL. Error: {ex.Message}";
        }
    }
}