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
[McpConfigurationRequirement("SerpApiKey", "SerpAPI API Key", isSecret: true,
    HelpUrl = "https://serpapi.com/manage-api-key")]
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
    /// Executes a search request against SerpAPI.
    /// </summary>
    private async Task<string> ExecuteSearchAsync(string engine, Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["SerpApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new McpException("SerpApiKey is not configured. Please set it in Settings.");
        }

        parameters["api_key"] = apiKey;
        if (!parameters.ContainsKey("engine"))
        {
            parameters["engine"] = engine;
        }

        var queryString = BuildQueryString(parameters);
        var requestUrl = $"/search.json?{queryString}";

        _logger.LogDebug("Request URL: {Url}", requestUrl);

        var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseContent;
    }

    /// <summary>
    /// Retrieves a stored search result by ID (for async searches).
    /// </summary>
    [McpServerTool, Description("Retrieve stored SerpAPI search results by search ID")]
    public async Task<string> GetSearchResult(
        [Description("Search ID from async search")]
        string searchId,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchId))
            {
                throw new McpException("Search ID is required.");
            }

            var apiKey = _configuration["SerpApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new McpException("SerpApiKey is not configured.");
            }

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
    /// Performs a general Google web search.
    /// </summary>
    [McpServerTool, Description("Search Google using SerpAPI")]
    public async Task<string> GoogleSearch(
        [Description("Search query")] string q,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Countries filter (e.g., countryFR|countryDE)"), DefaultValue("")]
        string cr = "",
        [Description("Languages filter (e.g., lang_fr|lang_de)"), DefaultValue("")]
        string lr = "",
        [Description(
             "Search type (tbm): (empty)=web, isch=images, lcl=local, vid=videos, nws=news, shop=shopping, pts=patents"),
         DefaultValue("")]
        string tbm = "",
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Advanced search filters (tbs)"), DefaultValue("")]
        string tbs = "",
        [Description("Safe search: active or off"), DefaultValue("")]
        string safe = "",
        [Description("Exclude auto-corrected results (1=exclude, 0=include)"), DefaultValue(null)]
        int? nfpr = null,
        [Description("Filter similar/omitted results (1=enabled default, 0=disabled)"), DefaultValue(null)]
        int? filter = null,
        [Description("Google CID (place identifier)"), DefaultValue("")]
        string ludocid = "",
        [Description("Knowledge graph map view force ID"), DefaultValue("")]
        string lsig = "",
        [Description("Knowledge Graph ID"), DefaultValue("")]
        string kgmid = "",
        [Description("Cached search parameters"), DefaultValue("")]
        string si = "",
        [Description("Layout/expansion parameters"), DefaultValue("")]
        string ibp = "",
        [Description("Search filter string from Google"), DefaultValue("")]
        string uds = "",
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            // Optional parameters - only add if they have a value
            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(cr)) parameters["cr"] = cr;
            if (!string.IsNullOrEmpty(lr)) parameters["lr"] = lr;
            if (!string.IsNullOrEmpty(tbm)) parameters["tbm"] = tbm;
            if (!string.IsNullOrEmpty(tbs)) parameters["tbs"] = tbs;
            if (!string.IsNullOrEmpty(safe)) parameters["safe"] = safe;
            if (nfpr.HasValue) parameters["nfpr"] = nfpr.Value.ToString(CultureInfo.InvariantCulture);
            if (filter.HasValue) parameters["filter"] = filter.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(ludocid)) parameters["ludocid"] = ludocid;
            if (!string.IsNullOrEmpty(lsig)) parameters["lsig"] = lsig;
            if (!string.IsNullOrEmpty(kgmid)) parameters["kgmid"] = kgmid;
            if (!string.IsNullOrEmpty(si)) parameters["si"] = si;
            if (!string.IsNullOrEmpty(ibp)) parameters["ibp"] = ibp;
            if (!string.IsNullOrEmpty(uds)) parameters["uds"] = uds;
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google", parameters);
            _logger.LogInformation("Completed Google Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Search");
            throw new McpException($"Failed to perform Google Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google Image search.
    /// </summary>
    [McpServerTool, Description("Search Google Images using SerpAPI")]
    public async Task<string> GoogleImageSearch(
        [Description("Search query for images")]
        string q,
        [Description("Number of results to return (max 100)"), DefaultValue(10)]
        int num = 10,
        [Description("Page index (0-based)"), DefaultValue(0)]
        int ijn = 0,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Countries filter (e.g., countryFR|countryDE)"), DefaultValue("")]
        string cr = "",
        [Description("Languages filter (e.g., lang_fr|lang_de)"), DefaultValue("")]
        string lr = "",
        [Description("Advanced search filters (tbs)"), DefaultValue("")]
        string tbs = "",
        [Description("Safe search: active or off"), DefaultValue("")]
        string safe = "",
        [Description("Exclude auto-corrected results (1=exclude, 0=include)"), DefaultValue(null)]
        int? nfpr = null,
        [Description("Filter similar/omitted results (1=enabled default, 0=disabled)"), DefaultValue(null)]
        int? filter = null,
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "num", num.ToString(CultureInfo.InvariantCulture) },
                { "ijn", ijn.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(cr)) parameters["cr"] = cr;
            if (!string.IsNullOrEmpty(lr)) parameters["lr"] = lr;
            if (!string.IsNullOrEmpty(tbs)) parameters["tbs"] = tbs;
            if (!string.IsNullOrEmpty(safe)) parameters["safe"] = safe;
            if (nfpr.HasValue) parameters["nfpr"] = nfpr.Value.ToString(CultureInfo.InvariantCulture);
            if (filter.HasValue) parameters["filter"] = filter.Value.ToString(CultureInfo.InvariantCulture);
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Image Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_images", parameters);
            _logger.LogInformation("Completed Google Image Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Image Search");
            throw new McpException($"Failed to perform Google Image Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google News search.
    /// </summary>
    [McpServerTool, Description("Search Google News using SerpAPI")]
    public async Task<string> GoogleNewsSearch(
        [Description("Search query for news")] string q,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Countries filter (e.g., countryFR|countryDE)"), DefaultValue("")]
        string cr = "",
        [Description("Languages filter (e.g., lang_fr|lang_de)"), DefaultValue("")]
        string lr = "",
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Advanced search filters (tbs)"), DefaultValue("")]
        string tbs = "",
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(cr)) parameters["cr"] = cr;
            if (!string.IsNullOrEmpty(lr)) parameters["lr"] = lr;
            if (!string.IsNullOrEmpty(tbs)) parameters["tbs"] = tbs;
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google News Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_news", parameters);
            _logger.LogInformation("Completed Google News Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google News Search");
            throw new McpException($"Failed to perform Google News Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google Videos search.
    /// </summary>
    [McpServerTool, Description("Search Google Videos using SerpAPI")]
    public async Task<string> GoogleVideosSearch(
        [Description("Search query for videos")]
        string q,
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Countries filter (e.g., countryFR|countryDE)"), DefaultValue("")]
        string cr = "",
        [Description("Languages filter (e.g., lang_fr|lang_de)"), DefaultValue("")]
        string lr = "",
        [Description("Advanced search filters (tbs)"), DefaultValue("")]
        string tbs = "",
        [Description("Safe search: active or off"), DefaultValue("")]
        string safe = "",
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(cr)) parameters["cr"] = cr;
            if (!string.IsNullOrEmpty(lr)) parameters["lr"] = lr;
            if (!string.IsNullOrEmpty(tbs)) parameters["tbs"] = tbs;
            if (!string.IsNullOrEmpty(safe)) parameters["safe"] = safe;
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Videos Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_videos", parameters);
            _logger.LogInformation("Completed Google Videos Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Videos Search");
            throw new McpException($"Failed to perform Google Videos Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google Shopping search.
    /// </summary>
    [McpServerTool, Description("Search Google Shopping using SerpAPI")]
    public async Task<string> GoogleShoppingSearch(
        [Description("Search query for shopping")]
        string q,
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Countries filter (e.g., countryFR|countryDE)"), DefaultValue("")]
        string cr = "",
        [Description("Languages filter (e.g., lang_fr|lang_de)"), DefaultValue("")]
        string lr = "",
        [Description("Advanced search filters (tbs)"), DefaultValue("")]
        string tbs = "",
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(cr)) parameters["cr"] = cr;
            if (!string.IsNullOrEmpty(lr)) parameters["lr"] = lr;
            if (!string.IsNullOrEmpty(tbs)) parameters["tbs"] = tbs;
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Shopping Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_shopping", parameters);
            _logger.LogInformation("Completed Google Shopping Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Shopping Search");
            throw new McpException($"Failed to perform Google Shopping Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google Local search.
    /// </summary>
    [McpServerTool, Description("Search Google Local using SerpAPI")]
    public async Task<string> GoogleLocalSearch(
        [Description("Search query for local results")]
        string q,
        [Description("Location for the search"), DefaultValue("")]
        string location = "",
        [Description("UULE encoded location"), DefaultValue("")]
        string uule = "",
        [Description("Latitude coordinate"), DefaultValue(null)]
        double? lat = null,
        [Description("Longitude coordinate"), DefaultValue(null)]
        double? lon = null,
        [Description("Search radius in meters"), DefaultValue(null)]
        int? radius = null,
        [Description("Google domain (e.g., google.com)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Country code (e.g., us, uk, fr)"), DefaultValue("us")]
        string gl = "us",
        [Description("Language code (e.g., en, es, fr)"), DefaultValue("en")]
        string hl = "en",
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "gl", gl },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(location)) parameters["location"] = location;
            if (!string.IsNullOrEmpty(uule)) parameters["uule"] = uule;
            if (lat.HasValue) parameters["lat"] = lat.Value.ToString(CultureInfo.InvariantCulture);
            if (lon.HasValue) parameters["lon"] = lon.Value.ToString(CultureInfo.InvariantCulture);
            if (radius.HasValue) parameters["radius"] = radius.Value.ToString(CultureInfo.InvariantCulture);
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Local Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_local", parameters);
            _logger.LogInformation("Completed Google Local Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Local Search");
            throw new McpException($"Failed to perform Google Local Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a Google Patents search.
    /// </summary>
    [McpServerTool, Description("Search Google Patents using SerpAPI")]
    public async Task<string> GooglePatentsSearch(
        [Description("Search query for patents")]
        string q,
        [Description("Result offset for pagination"), DefaultValue(0)]
        int start = 0,
        [Description("Country code for patents (e.g., US, EP, WO)"), DefaultValue("")]
        string country = "",
        [Description("Google domain (e.g., google.com/patents)"), DefaultValue("google.com")]
        string google_domain = "google.com",
        [Description("Language code (e.g., en)"), DefaultValue("en")]
        string hl = "en",
        [Description("Device type: desktop, tablet, mobile"), DefaultValue("desktop")]
        string device = "desktop",
        [Description("Bypass cache (true to disable)"), DefaultValue(false)]
        bool no_cache = false,
        [Description("Async mode (true to submit and retrieve later)"), DefaultValue(false)]
        bool async = false,
        [Description("ZeroTrace mode (Enterprise only, true to skip storing data)"), DefaultValue(false)]
        bool zero_trace = false,
        [Description("Output format: json or html"), DefaultValue("json")]
        string output = "json",
        [Description("JSON restrictor for field filtering"), DefaultValue("")]
        string json_restrictor = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new McpException("Query parameter 'q' is required.");
            }

            var parameters = new Dictionary<string, string>
            {
                { "q", q },
                { "start", start.ToString(CultureInfo.InvariantCulture) },
                { "device", device },
                { "google_domain", google_domain },
                { "hl", hl },
                { "output", output }
            };

            if (!string.IsNullOrEmpty(country)) parameters["country"] = country;
            if (no_cache) parameters["no_cache"] = "true";
            if (async) parameters["async"] = "true";
            if (zero_trace) parameters["zero_trace"] = "true";
            if (!string.IsNullOrEmpty(json_restrictor)) parameters["json_restrictor"] = json_restrictor;

            _logger.LogInformation("Executing Google Patents Search for: {Query}", q);
            var response = await ExecuteSearchAsync("google_patents", parameters);
            _logger.LogInformation("Completed Google Patents Search for: {Query}", q);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Patents Search");
            throw new McpException($"Failed to perform Google Patents Search: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds a query string from a dictionary of parameters, omitting empty values.
    /// </summary>
    private static string BuildQueryString(IDictionary<string, string> parameters)
    {
        return string.Join('&', parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }
}