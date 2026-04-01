using System.Net.Http.Json;
using System.Text.Json;

namespace Mullai.Tools.CodeSearchTool;

public class CodeSearchProvider(HttpClient httpClient)
{
    private const string BaseUrl = "https://mcp.exa.ai/mcp";

    public async Task<string> SearchCodeAsync(string query, int tokensNum = 5000)
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
                    name = "get_code_context_exa",
                    arguments = new
                    {
                        query, tokensNum
                    }
                }
            };

            var requestContent = JsonContent.Create(request);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
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
                return $"Error: Code search failed ({response.StatusCode}): {errorText}";
            }

            var responseText = await response.Content.ReadAsStringAsync();

            // The API response is SSE-like (data: ...). We need to parse it.
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

            return "No code context found for the query.";
        }
        catch (Exception ex)
        {
            return $"Failed to perform code search. Error: {ex.Message}";
        }
    }
}