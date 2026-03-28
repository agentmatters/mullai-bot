using System.ComponentModel;
using Mullai.Abstractions.Configuration;

using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol;

namespace Mullai.MCP.SerpApi.Tools;

[McpServerToolType]
[McpConfigurationRequirement("SerpApiKey", "SerpAPI API Key", isSecret: true, HelpUrl = "https://serpapi.com/manage-api-key")]

public sealed class SerpApiTools
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SerpApiTools> _logger;
    private readonly HttpClient _httpClient;

    public SerpApiTools(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SerpApiTools> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://serpapi.com");
    }

    [McpServerTool, Description("Search Google using SerpAPI")]
    public async Task<string> GoogleSearch(
        HttpClient client,
        [Description("Search query")] 
        string query)
    {
        try
        {
            var apiKey = _configuration["SerpApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new McpException("SerpApiKey is not configured. Please set it in Settings.");
            }

            _logger.LogInformation("Executing Google Search for: {Query}", query);


            var searchParameters = new Dictionary<string, string>
            {
                {"q", query},
                {"api_key", apiKey}
            };

            var requestUrl = $"/search.json?{string.Join('&', searchParameters
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))}";

            _logger.LogDebug("Request URL: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response for query: {Query}", query);

            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Search");
            throw new McpException($"Failed to perform Google Search: {ex.Message}");
        }
    }

    [McpServerTool, Description("Search Google Images using SerpAPI")]
    public async Task<string> GoogleImageSearch(
        HttpClient client,
        [Description("Search query for images")] 
        string query,
        [Description("Number of results to return"), DefaultValue(10)]
        int numResults = 10)
    {
        try
        {
            var apiKey = _configuration["SerpApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new McpException("SerpApiKey is not configured. Please set it in Settings.");
            }

            _logger.LogInformation("Executing Google Image Search for: {Query}", query);


            var searchParameters = new Dictionary<string, string>
            {
                {"q", query},
                {"api_key", apiKey},
                {"engine", "google_images"},
                {"ijn", "0"},
                {"start", "0"},
                {"num", numResults.ToString(CultureInfo.InvariantCulture)}
            };

            var requestUrl = $"/search.json?{string.Join('&', searchParameters
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))}";

            _logger.LogDebug("Request URL: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response for image search: {Query}", query);

            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Image Search");
            throw new McpException($"Failed to perform Google Image Search: {ex.Message}");
        }
    }
}

// Extension method for HttpClient
public static class HttpClientExtensions
{
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}