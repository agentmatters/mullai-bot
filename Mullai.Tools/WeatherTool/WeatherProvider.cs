namespace Mullai.Tools.WeatherTool;

/// <summary>
/// The weather provider that returns weather information.
/// </summary>
public class WeatherProvider
{
    /// <summary>
    /// Gets the weather information for the specified location.
    /// </summary>
    /// <remarks>
    /// The weather information is hardcoded for demonstration purposes.
    /// In a real application, this could call a weather API to get actual weather data.
    /// </remarks>
    /// <param name="location">The location to get the weather for.</param>
    /// <returns>The weather information for the specified location.</returns>
    public string GetWeather(string location)
    {
        return $"The weather in {location} is cloudy with a high of 15°C.";
    }
}