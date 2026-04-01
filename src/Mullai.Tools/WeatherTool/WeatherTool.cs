using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Tools.WeatherTool.Models;

namespace Mullai.Tools.WeatherTool;

/// <summary>
///     The agent plugin that provides weather, geolocation, and current time information.
/// </summary>
[Description(
    "A tool for retrieving weather information, geographic coordinates, and local time for any location worldwide.")]
public class WeatherTool(WeatherProvider weatherProvider, GeolocationProvider geolocationProvider)
{
    /// <summary>
    ///     Gets the geographic coordinates (latitude and longitude) for a specified location.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A formatted string with the location details including coordinates.</returns>
    [Description(
        "Resolves a location name (like 'London' or 'Seattle') into geographic coordinates (latitude and longitude) and other metadata like elevation and timezone.")]
    public async Task<string> GetLocationCoordinatesAsync(
        [Description("The name of the city, landmark, or region to lookup.")]
        string locationName)
    {
        var result = await geolocationProvider.SearchFirstAsync(locationName);

        if (result == null) return $"Location '{locationName}' not found.";

        var details = $"Location: {result.Name}, Country: {result.Country}";
        if (!string.IsNullOrEmpty(result.Admin1)) details += $", State: {result.Admin1}";

        details += $"\nCoordinates: Latitude {result.Latitude:F4}, Longitude {result.Longitude:F4}";

        if (result.Elevation.HasValue) details += $"\nElevation: {result.Elevation}m";

        if (!string.IsNullOrEmpty(result.Timezone)) details += $"\nTimezone: {result.Timezone}";

        return details;
    }

    /// <summary>
    ///     Gets all geolocation results for a specified location name.
    /// </summary>
    /// <param name="locationName">The name of the location to search for.</param>
    /// <returns>A list of matching geolocation results.</returns>
    [Description(
        "Searches for multiple matching locations by name and returns a list of potential candidates with their coordinates.")]
    public async Task<List<GeolocationResult>> SearchLocationsAsync(
        [Description("The name of the location to search for.")]
        string locationName)
    {
        return await geolocationProvider.SearchAsync(locationName);
    }

    /// <summary>
    ///     Gets the weather information for the specified location.
    /// </summary>
    /// <remarks>
    ///     This method demonstrates how to use the dependency that was injected into the plugin class.
    /// </remarks>
    /// <param name="location">The location to get the weather for.</param>
    /// <returns>The weather information for the specified location.</returns>
    [Description("Retrieves the current weather conditions (temperature, humidity, description) for a given location.")]
    public string GetWeather(
        [Description("The name of the location to get weather for.")]
        string location)
    {
        return weatherProvider.GetWeather(location);
    }

    /// <summary>
    ///     Gets the current date and time for the specified location.
    /// </summary>
    /// <remarks>
    ///     This method demonstrates how to resolve a dependency using the service provider passed to the method.
    /// </remarks>
    /// <param name="sp">The service provider to resolve the <see cref="CurrentTimeProvider" />.</param>
    /// <param name="location">The location to get the current time for.</param>
    /// <returns>The current date and time as a <see cref="DateTimeOffset" />.</returns>
    [Description("Returns the current date and time for a specific location, accounting for its local timezone.")]
    public DateTimeOffset GetCurrentTime(
        IServiceProvider sp,
        [Description("The name of the location to get the current time for.")]
        string location)
    {
        // Resolve the CurrentTimeProvider from the service provider
        var currentTimeProvider = sp.GetRequiredService<CurrentTimeProvider>();

        return currentTimeProvider.GetCurrentTime(location);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    /// <remarks>
    ///     In real world scenarios, a class may have many methods and only a subset of them may be intended to be exposed as
    ///     AI functions.
    ///     This method demonstrates how to explicitly specify which methods should be exposed to the AI agent.
    /// </remarks>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(GetLocationCoordinatesAsync);
        yield return AIFunctionFactory.Create(SearchLocationsAsync);
        yield return AIFunctionFactory.Create(GetWeather);
        yield return AIFunctionFactory.Create(GetCurrentTime);
    }
}