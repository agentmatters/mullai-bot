using System.Text.Json;
using Mullai.Tools.WeatherTool.Models;

namespace Mullai.Tools.WeatherTool;

/// <summary>
/// Provider for retrieving geolocation data from the Open-Meteo Geocoding API.
/// </summary>
public class GeolocationProvider
{
    private const string BaseUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    public GeolocationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Searches for geolocation data for a given location name.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A list of geolocation results matching the search query.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    /// <exception cref="JsonException">Thrown when the response cannot be deserialized.</exception>
    public async Task<List<GeolocationResult>> SearchAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            throw new ArgumentException("Location name cannot be null or empty.", nameof(locationName));
        }

        var url = $"{BaseUrl}?name={Uri.EscapeDataString(locationName)}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var geolocationResponse = JsonSerializer.Deserialize<GeolocationResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return geolocationResponse?.Results ?? new List<GeolocationResult>();
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to fetch geolocation data for '{locationName}' from the API.", ex);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to deserialize geolocation response for '{locationName}'.", ex);
        }
    }

    /// <summary>
    /// Searches for the primary geolocation result for a given location name.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>The first geolocation result, or null if no results are found.</returns>
    public async Task<GeolocationResult?> SearchFirstAsync(string locationName)
    {
        var results = await SearchAsync(locationName);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Gets the latitude and longitude for a given location name.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A tuple containing the latitude and longitude, or null if the location is not found.</returns>
    public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string locationName)
    {
        var result = await SearchFirstAsync(locationName);
        
        if (result == null)
        {
            return null;
        }

        return (result.Latitude, result.Longitude);
    }
}
