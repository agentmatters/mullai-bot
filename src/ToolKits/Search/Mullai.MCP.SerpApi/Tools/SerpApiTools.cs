using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol;
using Mullai.Abstractions.Configuration;

namespace Mullai.MCP.SerpApi.Tools;

/// <summary>
/// Tool for interacting with the SerpAPI to perform searches.
/// </summary>
[McpServerToolType]
[McpConfigurationRequirement("SerpApiKey", "SerpAPI API Key", isSecret: true, HelpUrl = "https://serpapi.com/manage-api-key")]
public sealed class SerpApiTools
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SerpApiTools> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpApiTools"/> class.
    /// </summary>
    public SerpApiTools(HttpClient httpClient, IConfiguration configuration, ILogger<SerpApiTools> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://serpapi.com");
    }

    /// <summary>
    /// Performs a Google search using SerpAPI.
    /// </summary>
    [McpServerTool, Description("Search Google using SerpAPI")]
    public async Task<string> GoogleSearch(
        [Description("Search query")] string query,
        [Description("Location for the search"), DefaultValue("")] string location = "",
        [Description("Device type: desktop, tablet, or mobile"), DefaultValue("desktop")] string device = "desktop",
        [Description("Country code for the search"), DefaultValue("us")] string gl = "us",
        [Description("Language code for the search"), DefaultValue("en")] string hl = "en",
        [Description("Start index for pagination"), DefaultValue(0)] int start = 0)
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
                {"api_key", apiKey},
                {"location", location},
                {"device", device},
                {"gl", gl},
                {"hl", hl},
                {"start", start.ToString(CultureInfo.InvariantCulture)}
            };

            var requestUrl = $"/search.json?{BuildQueryString(searchParameters)}";

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

    /// <summary>
    /// Performs a Google Image search using SerpAPI.
    /// </summary>
    [McpServerTool, Description("Search Google Images using SerpAPI")]
    public async Task<string> GoogleImageSearch(
        [Description("Search query for images")] string query,
        [Description("Number of results to return"), DefaultValue(10)] int numResults = 10,
        [Description("Location for the search"), DefaultValue("")] string location = "",
        [Description("Device type: desktop, tablet, or mobile"), DefaultValue("desktop")] string device = "desktop",
        [Description("Country code for the search"), DefaultValue("us")] string gl = "us",
        [Description("Language code for the search"), DefaultValue("en")] string hl = "en")
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
                {"num", numResults.ToString(CultureInfo.InvariantCulture)},
                {"location", location},
                {"device", device},
                {"gl", gl},
                {"hl", hl}
            };

            var requestUrl = $"/search.json?{BuildQueryString(searchParameters)}";

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

    /// <summary>
    /// Builds a query string from a dictionary of parameters.
    /// </summary>
    private static string BuildQueryString(IDictionary<string, string> parameters)
    {
        return string.Join('&', parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }
}