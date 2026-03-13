namespace Mullai.Tools.WeatherTool.Models;

/// <summary>
/// Represents the response from the Open-Meteo geocoding API.
/// </summary>
public class GeolocationResponse
{
    /// <summary>
    /// Gets or sets the list of geolocation results.
    /// </summary>
    public List<GeolocationResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the generation time in milliseconds.
    /// </summary>
    public double GenerationtimeMs { get; set; }
}
