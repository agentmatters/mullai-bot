using System.Text.Json.Serialization;

namespace Mullai.Tools.WeatherTool.Models;

/// <summary>
/// Represents the response from the Open-Meteo weather forecast API.
/// </summary>
public class WeatherForecastResponse
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationtimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; } = string.Empty;

    public double Elevation { get; set; }

    [JsonPropertyName("hourly_units")]
    public HourlyUnits HourlyUnits { get; set; } = new();

    public HourlyData Hourly { get; set; } = new();
}
