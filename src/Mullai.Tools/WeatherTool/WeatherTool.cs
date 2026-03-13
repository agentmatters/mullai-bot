using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Tools.WeatherTool.Models;

namespace Mullai.Tools.WeatherTool;

/// <summary>
/// The agent plugin that provides weather, geolocation, and current time information.
/// </summary>
/// <param name="weatherProvider">The weather provider to get weather information.</param>
/// <param name="geolocationProvider">The geolocation provider to get location coordinates.</param>
public class WeatherTool(WeatherProvider weatherProvider, GeolocationProvider geolocationProvider)
{
    /// <summary>
    /// Gets the geographic coordinates (latitude and longitude) for a specified location.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A formatted string with the location details including coordinates.</returns>
    public async Task<string> GetLocationCoordinatesAsync(string locationName)
    {
        var result = await geolocationProvider.SearchFirstAsync(locationName);
        
        if (result == null)
        {
            return $"Location '{locationName}' not found.";
        }

        var details = $"Location: {result.Name}, Country: {result.Country}";
        if (!string.IsNullOrEmpty(result.Admin1))
        {
            details += $", State: {result.Admin1}";
        }

        details += $"\nCoordinates: Latitude {result.Latitude:F4}, Longitude {result.Longitude:F4}";
        
        if (result.Elevation.HasValue)
        {
            details += $"\nElevation: {result.Elevation}m";
        }

        if (!string.IsNullOrEmpty(result.Timezone))
        {
            details += $"\nTimezone: {result.Timezone}";
        }

        return details;
    }

    /// <summary>
    /// Gets all geolocation results for a specified location name.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A list of matching geolocation results.</returns>
    public async Task<List<GeolocationResult>> SearchLocationsAsync(string locationName)
    {
        return await geolocationProvider.SearchAsync(locationName);
    }

    /// <summary>
    /// Gets the weather information for the specified location.
    /// </summary>
    /// <remarks>
    /// This method demonstrates how to use the dependency that was injected into the plugin class.
    /// </remarks>
    /// <param name="location">The location to get the weather for.</param>
    /// <returns>The weather information for the specified location.</returns>
    public string GetWeather(string location)
    {
        return weatherProvider.GetWeather(location);
    }

    /// <summary>
    /// Gets the current date and time for the specified location.
    /// </summary>
    /// <remarks>
    /// This method demonstrates how to resolve a dependency using the service provider passed to the method.
    /// </remarks>
    /// <param name="sp">The service provider to resolve the <see cref="CurrentTimeProvider"/>.</param>
    /// <param name="location">The location to get the current time for.</param>
    /// <returns>The current date and time as a <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset GetCurrentTime(IServiceProvider sp, string location)
    {
        // Resolve the CurrentTimeProvider from the service provider
        var currentTimeProvider = sp.GetRequiredService<CurrentTimeProvider>();

        return currentTimeProvider.GetCurrentTime(location);
    }

    /// <summary>
    /// Returns the functions provided by this plugin.
    /// </summary>
    /// <remarks>
    /// In real world scenarios, a class may have many methods and only a subset of them may be intended to be exposed as AI functions.
    /// This method demonstrates how to explicitly specify which methods should be exposed to the AI agent.
    /// </remarks>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.GetLocationCoordinatesAsync);
        yield return AIFunctionFactory.Create(this.SearchLocationsAsync);
        yield return AIFunctionFactory.Create(this.GetWeather);
        yield return AIFunctionFactory.Create(this.GetCurrentTime);
    }
}