namespace Mullai.Tools.WeatherTool.Models;

/// <summary>
/// Represents a geolocation result from the Open-Meteo geocoding API.
/// </summary>
public class GeolocationResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this location.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the location.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latitude of the location.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the location.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the elevation of the location in meters.
    /// </summary>
    public double? Elevation { get; set; }

    /// <summary>
    /// Gets or sets the feature code (e.g., "PPLA" for principal political area).
    /// </summary>
    public string? FeatureCode { get; set; }

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the primary administrative division (state/province).
    /// </summary>
    public string? Admin1 { get; set; }

    /// <summary>
    /// Gets or sets the secondary administrative division.
    /// </summary>
    public string? Admin2 { get; set; }

    /// <summary>
    /// Gets or sets the timezone for this location.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets the population of this location.
    /// </summary>
    public long? Population { get; set; }
}
