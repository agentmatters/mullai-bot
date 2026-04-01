using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mullai.MCP.SerpApi.Models;

/// <summary>
///     Base class for SerpAPI search requests containing common parameters.
/// </summary>
public abstract class SerpApiSearchRequest
{
    [Required]
    [Description("Search query")]
    public string Q { get; set; } = string.Empty;

    [Description("Location for the search")]
    [DefaultValue("")]
    public string Location { get; set; } = string.Empty;

    [Description("UULE encoded location")]
    [DefaultValue("")]
    public string Uule { get; set; } = string.Empty;

    [Description("Latitude coordinate")]
    [DefaultValue(null)]
    public double? Lat { get; set; }

    [Description("Longitude coordinate")]
    [DefaultValue(null)]
    public double? Lon { get; set; }

    [Description("Search radius in meters")]
    [DefaultValue(null)]
    public int? Radius { get; set; }

    [Description("Google domain (e.g., google.com)")]
    [DefaultValue("google.com")]
    public string GoogleDomain { get; set; } = "google.com";

    [Description("Country code (e.g., us, uk, fr)")]
    [DefaultValue("us")]
    public string Gl { get; set; } = "us";

    [Description("Language code (e.g., en, es, fr)")]
    [DefaultValue("en")]
    public string Hl { get; set; } = "en";

    [Description("Advanced search filters (tbs)")]
    [DefaultValue("")]
    public string Tbs { get; set; } = string.Empty;

    [Description("Device type: desktop, tablet, mobile")]
    [DefaultValue("desktop")]
    public string Device { get; set; } = "desktop";

    [Description("Bypass cache (true to disable)")]
    [DefaultValue(false)]
    public bool NoCache { get; set; } = false;

    [Description("Async mode (true to submit and retrieve later)")]
    [DefaultValue(false)]
    public bool Async { get; set; } = false;

    [Description("ZeroTrace mode (Enterprise only, true to skip storing data)")]
    [DefaultValue(false)]
    public bool ZeroTrace { get; set; } = false;

    [Description("Output format: json or html")]
    [DefaultValue("json")]
    public string Output { get; set; } = "json";

    [Description("JSON restrictor for field filtering")]
    [DefaultValue("")]
    public string JsonRestrictor { get; set; } = string.Empty;
}

/// <summary>
///     Request model for Google web search.
/// </summary>
public class GoogleSearchRequest : SerpApiSearchRequest
{
    [Description("Countries filter (e.g., countryFR|countryDE)")]
    [DefaultValue("")]
    public string Cr { get; set; } = string.Empty;

    [Description("Languages filter (e.g., lang_fr|lang_de)")]
    [DefaultValue("")]
    public string Lr { get; set; } = string.Empty;

    [Description(
        "Search type (tbm): (empty)=web, isch=images, lcl=local, vid=videos, nws=news, shop=shopping, pts=patents")]
    [DefaultValue("")]
    public string Tbm { get; set; } = string.Empty;

    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;

    [Description("Safe search: active or off")]
    [DefaultValue("")]
    public string Safe { get; set; } = string.Empty;

    [Description("Exclude auto-corrected results (1=exclude, 0=include)")]
    [DefaultValue(null)]
    public int? Nfpr { get; set; }

    [Description("Filter similar/omitted results (1=enabled default, 0=disabled)")]
    [DefaultValue(null)]
    public int? Filter { get; set; }

    [Description("Google CID (place identifier)")]
    [DefaultValue("")]
    public string Ludocid { get; set; } = string.Empty;

    [Description("Knowledge graph map view force ID")]
    [DefaultValue("")]
    public string Lsig { get; set; } = string.Empty;

    [Description("Knowledge Graph ID")]
    [DefaultValue("")]
    public string Kgmid { get; set; } = string.Empty;

    [Description("Cached search parameters")]
    [DefaultValue("")]
    public string Si { get; set; } = string.Empty;

    [Description("Layout/expansion parameters")]
    [DefaultValue("")]
    public string Ibp { get; set; } = string.Empty;

    [Description("Search filter string from Google")]
    [DefaultValue("")]
    public string Uds { get; set; } = string.Empty;
}

/// <summary>
///     Request model for Google Image search.
/// </summary>
public class GoogleImageSearchRequest : SerpApiSearchRequest
{
    [Description("Number of results to return (max 100)")]
    [DefaultValue(10)]
    public int Num { get; set; } = 10;

    [Description("Page index (0-based)")]
    [DefaultValue(0)]
    public int Ijn { get; set; } = 0;

    [Description("Countries filter (e.g., countryFR|countryDE)")]
    [DefaultValue("")]
    public string Cr { get; set; } = string.Empty;

    [Description("Languages filter (e.g., lang_fr|lang_de)")]
    [DefaultValue("")]
    public string Lr { get; set; } = string.Empty;

    [Description("Safe search: active or off")]
    [DefaultValue("")]
    public string Safe { get; set; } = string.Empty;

    [Description("Exclude auto-corrected results (1=exclude, 0=include)")]
    [DefaultValue(null)]
    public int? Nfpr { get; set; }

    [Description("Filter similar/omitted results (1=enabled default, 0=disabled)")]
    [DefaultValue(null)]
    public int? Filter { get; set; }
}

/// <summary>
///     Request model for Google News search.
/// </summary>
public class GoogleNewsSearchRequest : SerpApiSearchRequest
{
    [Description("Countries filter (e.g., countryFR|countryDE)")]
    [DefaultValue("")]
    public string Cr { get; set; } = string.Empty;

    [Description("Languages filter (e.g., lang_fr|lang_de)")]
    [DefaultValue("")]
    public string Lr { get; set; } = string.Empty;

    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;
}

/// <summary>
///     Request model for Google Videos search.
/// </summary>
public class GoogleVideosSearchRequest : SerpApiSearchRequest
{
    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;

    [Description("Countries filter (e.g., countryFR|countryDE)")]
    [DefaultValue("")]
    public string Cr { get; set; } = string.Empty;

    [Description("Languages filter (e.g., lang_fr|lang_de)")]
    [DefaultValue("")]
    public string Lr { get; set; } = string.Empty;

    [Description("Safe search: active or off")]
    [DefaultValue("")]
    public string Safe { get; set; } = string.Empty;
}

/// <summary>
///     Request model for Google Shopping search.
/// </summary>
public class GoogleShoppingSearchRequest : SerpApiSearchRequest
{
    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;

    [Description("Countries filter (e.g., countryFR|countryDE)")]
    [DefaultValue("")]
    public string Cr { get; set; } = string.Empty;

    [Description("Languages filter (e.g., lang_fr|lang_de)")]
    [DefaultValue("")]
    public string Lr { get; set; } = string.Empty;
}

/// <summary>
///     Request model for Google Local search.
/// </summary>
public class GoogleLocalSearchRequest : SerpApiSearchRequest
{
    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;
}

/// <summary>
///     Request model for Google Patents search.
/// </summary>
public class GooglePatentsSearchRequest : SerpApiSearchRequest
{
    [Description("Result offset for pagination")]
    [DefaultValue(0)]
    public int Start { get; set; } = 0;

    [Description("Country code for patents (e.g., US, EP, WO)")]
    [DefaultValue("")]
    public string Country { get; set; } = string.Empty;
}