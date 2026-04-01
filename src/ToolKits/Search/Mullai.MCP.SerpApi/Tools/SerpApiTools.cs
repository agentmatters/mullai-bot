using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mullai.Abstractions.Configuration;
using Mullai.MCP.SerpApi.Models;

namespace Mullai.MCP.SerpApi.Tools;

/// <summary>
///     Tool for interacting with the SerpAPI to perform searches.
/// </summary>
[McpServerToolType]
[McpConfigurationRequirement("SerpApiKey", "SerpAPI API Key", true,
    HelpUrl = "https://serpapi.com/manage-api-key")]
public sealed class SerpApiTools
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SerpApiTools> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SerpApiTools" /> class.
    /// </summary>
    public SerpApiTools(HttpClient httpClient, IConfiguration configuration, ILogger<SerpApiTools> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://serpapi.com");
    }

    /// <summary>
    ///     Executes a search request against SerpAPI.
    /// </summary>
    private async Task<string> ExecuteSearchAsync(string engine, Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["SerpApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new McpException("SerpApiKey is not configured. Please set it in Settings.");

        parameters["api_key"] = apiKey;
        if (!parameters.ContainsKey("engine")) parameters["engine"] = engine;

        var queryString = BuildQueryString(parameters);
        var requestUrl = $"/search.json?{queryString}";

        _logger.LogDebug("Request URL: {Url}", requestUrl);

        var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseContent;
    }

    /// <summary>
    ///     Retrieves a stored search result by ID (for async searches).
    /// </summary>
    [McpServerTool]
    [Description("Retrieve stored SerpAPI search results by search ID")]
    public async Task<string> GetSearchResult(
        [Description("Search ID from async search")]
        string searchId,
        [Description("Output format: json or html")] [DefaultValue("json")]
        string output = "json")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchId)) throw new McpException("Search ID is required.");

            var apiKey = _configuration["SerpApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw new McpException("SerpApiKey is not configured.");

            var requestUrl = $"/searches/{searchId}.json?api_key={apiKey}&output={output}";
            _logger.LogDebug("Request URL: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving search result for ID: {SearchId}", searchId);
            throw new McpException($"Failed to retrieve search result: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a general Google web search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google using SerpAPI")]
    public async Task<string> GoogleSearch(GoogleSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Cr)) parameters["cr"] = request.Cr;
            if (!string.IsNullOrEmpty(request.Lr)) parameters["lr"] = request.Lr;
            if (!string.IsNullOrEmpty(request.Tbm)) parameters["tbm"] = request.Tbm;
            if (!string.IsNullOrEmpty(request.Safe)) parameters["safe"] = request.Safe;
            if (request.Nfpr.HasValue) parameters["nfpr"] = request.Nfpr.Value.ToString(CultureInfo.InvariantCulture);
            if (request.Filter.HasValue)
                parameters["filter"] = request.Filter.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Ludocid)) parameters["ludocid"] = request.Ludocid;
            if (!string.IsNullOrEmpty(request.Lsig)) parameters["lsig"] = request.Lsig;
            if (!string.IsNullOrEmpty(request.Kgmid)) parameters["kgmid"] = request.Kgmid;
            if (!string.IsNullOrEmpty(request.Si)) parameters["si"] = request.Si;
            if (!string.IsNullOrEmpty(request.Ibp)) parameters["ibp"] = request.Ibp;
            if (!string.IsNullOrEmpty(request.Uds)) parameters["uds"] = request.Uds;

            _logger.LogInformation("Executing Google Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google", parameters);
            _logger.LogInformation("Completed Google Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Search");
            throw new McpException($"Failed to perform Google Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google Image search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google Images using SerpAPI")]
    public async Task<string> GoogleImageSearch(GoogleImageSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["num"] = request.Num.ToString(CultureInfo.InvariantCulture);
            parameters["ijn"] = request.Ijn.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Cr)) parameters["cr"] = request.Cr;
            if (!string.IsNullOrEmpty(request.Lr)) parameters["lr"] = request.Lr;
            if (!string.IsNullOrEmpty(request.Safe)) parameters["safe"] = request.Safe;
            if (request.Nfpr.HasValue) parameters["nfpr"] = request.Nfpr.Value.ToString(CultureInfo.InvariantCulture);
            if (request.Filter.HasValue)
                parameters["filter"] = request.Filter.Value.ToString(CultureInfo.InvariantCulture);

            _logger.LogInformation("Executing Google Image Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_images", parameters);
            _logger.LogInformation("Completed Google Image Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Image Search");
            throw new McpException($"Failed to perform Google Image Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google News search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google News using SerpAPI")]
    public async Task<string> GoogleNewsSearch(GoogleNewsSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Cr)) parameters["cr"] = request.Cr;
            if (!string.IsNullOrEmpty(request.Lr)) parameters["lr"] = request.Lr;

            _logger.LogInformation("Executing Google News Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_news", parameters);
            _logger.LogInformation("Completed Google News Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google News Search");
            throw new McpException($"Failed to perform Google News Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google Videos search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google Videos using SerpAPI")]
    public async Task<string> GoogleVideosSearch(GoogleVideosSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Cr)) parameters["cr"] = request.Cr;
            if (!string.IsNullOrEmpty(request.Lr)) parameters["lr"] = request.Lr;
            if (!string.IsNullOrEmpty(request.Safe)) parameters["safe"] = request.Safe;

            _logger.LogInformation("Executing Google Videos Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_videos", parameters);
            _logger.LogInformation("Completed Google Videos Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Videos Search");
            throw new McpException($"Failed to perform Google Videos Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google Shopping search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google Shopping using SerpAPI")]
    public async Task<string> GoogleShoppingSearch(GoogleShoppingSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Cr)) parameters["cr"] = request.Cr;
            if (!string.IsNullOrEmpty(request.Lr)) parameters["lr"] = request.Lr;

            _logger.LogInformation("Executing Google Shopping Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_shopping", parameters);
            _logger.LogInformation("Completed Google Shopping Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Shopping Search");
            throw new McpException($"Failed to perform Google Shopping Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google Local search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google Local using SerpAPI")]
    public async Task<string> GoogleLocalSearch(GoogleLocalSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);

            _logger.LogInformation("Executing Google Local Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_local", parameters);
            _logger.LogInformation("Completed Google Local Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Local Search");
            throw new McpException($"Failed to perform Google Local Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Performs a Google Patents search.
    /// </summary>
    [McpServerTool]
    [Description("Search Google Patents using SerpAPI")]
    public async Task<string> GooglePatentsSearch(GooglePatentsSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Q)) throw new McpException("Query parameter 'q' is required.");

            var parameters = new Dictionary<string, string>();
            MapBaseParameters(request, parameters);

            parameters["start"] = request.Start.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(request.Country)) parameters["country"] = request.Country;

            _logger.LogInformation("Executing Google Patents Search for: {Query}", request.Q);
            var response = await ExecuteSearchAsync("google_patents", parameters);
            _logger.LogInformation("Completed Google Patents Search for: {Query}", request.Q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Patents Search");
            throw new McpException($"Failed to perform Google Patents Search: {ex.Message}");
        }
    }

    /// <summary>
    ///     Maps common search parameters from the request to the parameters dictionary.
    /// </summary>
    private static void MapBaseParameters(SerpApiSearchRequest request, Dictionary<string, string> parameters)
    {
        parameters["q"] = request.Q;
        if (!string.IsNullOrEmpty(request.Location)) parameters["location"] = request.Location;
        if (!string.IsNullOrEmpty(request.Uule)) parameters["uule"] = request.Uule;
        if (request.Lat.HasValue) parameters["lat"] = request.Lat.Value.ToString(CultureInfo.InvariantCulture);
        if (request.Lon.HasValue) parameters["lon"] = request.Lon.Value.ToString(CultureInfo.InvariantCulture);
        if (request.Radius.HasValue) parameters["radius"] = request.Radius.Value.ToString(CultureInfo.InvariantCulture);
        parameters["google_domain"] = request.GoogleDomain;
        parameters["gl"] = request.Gl;
        parameters["hl"] = request.Hl;
        if (!string.IsNullOrEmpty(request.Tbs)) parameters["tbs"] = request.Tbs;
        parameters["device"] = request.Device;
        parameters["output"] = request.Output;
        if (request.NoCache) parameters["no_cache"] = "true";
        if (request.Async) parameters["async"] = "true";
        if (request.ZeroTrace) parameters["zero_trace"] = "true";
        if (!string.IsNullOrEmpty(request.JsonRestrictor)) parameters["json_restrictor"] = request.JsonRestrictor;
    }

    /// <summary>
    ///     Builds a query string from a dictionary of parameters, omitting empty values.
    /// </summary>
    private static string BuildQueryString(IDictionary<string, string> parameters)
    {
        return string.Join('&', parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }
}