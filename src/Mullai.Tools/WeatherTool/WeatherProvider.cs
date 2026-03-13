using System.Text.Json;
using Mullai.Tools.WeatherTool.Models;

namespace Mullai.Tools.WeatherTool;

/// <summary>
/// Provider for retrieving weather forecast data from the Open-Meteo API.
/// </summary>
public class WeatherProvider
{
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
    private readonly HttpClient _httpClient;
    private readonly GeolocationProvider _geolocationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    /// <param name="geolocationProvider">The geolocation provider to resolve location names to coordinates.</param>
    public WeatherProvider(HttpClient httpClient, GeolocationProvider geolocationProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _geolocationProvider = geolocationProvider ?? throw new ArgumentNullException(nameof(geolocationProvider));
    }

    /// <summary>
    /// Gets the weather information for the specified location.
    /// </summary>
    /// <param name="location">The location name (e.g., "Chennai", "New York").</param>
    /// <returns>A formatted string with current weather information.</returns>
    public string GetWeather(string location)
    {
        try
        {
            var task = GetWeatherAsync(location);
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            return $"Error retrieving weather for {location}: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the weather forecast asynchronously for the specified location.
    /// </summary>
    /// <param name="location">The location name (e.g., "Chennai", "New York").</param>
    /// <returns>A formatted string with weather forecast information.</returns>
    public async Task<string> GetWeatherAsync(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be null or empty.", nameof(location));
        }

        // Get coordinates from location name
        var coordinates = await _geolocationProvider.GetCoordinatesAsync(location);
        if (coordinates == null)
        {
            return $"Location '{location}' not found.";
        }

        var (latitude, longitude) = coordinates.Value;

        // Get weather data
        var forecast = await GetWeatherForecastAsync(latitude, longitude);
        
        if (forecast == null || forecast.Hourly.Time.Count == 0)
        {
            return $"No weather data available for {location}.";
        }

        return FormatWeatherData(location, forecast, latitude, longitude);
    }

    /// <summary>
    /// Gets the weather forecast for specific coordinates.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <returns>The weather forecast response, or null if the request fails.</returns>
    private async Task<WeatherForecastResponse?> GetWeatherForecastAsync(double latitude, double longitude)
    {
        var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,relative_humidity_2m,rain,snowfall,apparent_temperature,dew_point_2m,precipitation_probability,precipitation,showers,snow_depth,weather_code,pressure_msl,surface_pressure,cloud_cover_low,cloud_cover,cloud_cover_mid,cloud_cover_high,visibility,evapotranspiration,et0_fao_evapotranspiration,vapour_pressure_deficit,temperature_180m,temperature_120m,temperature_80m,wind_gusts_10m,wind_direction_180m,wind_direction_80m,wind_direction_120m,wind_direction_10m,wind_speed_180m,wind_speed_120m,wind_speed_80m,wind_speed_10m,soil_temperature_0cm,soil_temperature_6cm,soil_temperature_18cm,soil_temperature_54cm,soil_moisture_0_to_1cm,soil_moisture_1_to_3cm,soil_moisture_9_to_27cm,soil_moisture_27_to_81cm,soil_moisture_3_to_9cm,uv_index,uv_index_clear_sky,is_day,sunshine_duration,wet_bulb_temperature_2m,total_column_integrated_water_vapour,boundary_layer_height,freezing_level_height,convective_inhibition,lifted_index,cape&forecast_days=1&format=json&timeformat=unixtime";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var forecast = JsonSerializer.Deserialize<WeatherForecastResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return forecast;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Failed to fetch weather data from the API.", ex);
        }
    }

    /// <summary>
    /// Formats the weather forecast data into a human-readable string.
    /// </summary>
    private static string FormatWeatherData(string location, WeatherForecastResponse forecast, double latitude, double longitude)
    {
        var result = new System.Text.StringBuilder();
        
        result.AppendLine($"Weather Forecast for {location}");
        result.AppendLine($"Coordinates: {latitude:F4}°N, {longitude:F4}°E");
        result.AppendLine($"Timezone: {forecast.Timezone}");
        result.AppendLine($"Elevation: {forecast.Elevation}m");
        result.AppendLine();

        // Get the latest (first) hourly data point
        if (forecast.Hourly.Time.Count > 0)
        {
            var timestamp = UnixTimeStampToDateTime(forecast.Hourly.Time[0]);
            var temperature = forecast.Hourly.Temperature2m?[0];
            var humidity = forecast.Hourly.RelativeHumidity2m?[0];
            var apparentTemp = forecast.Hourly.ApparentTemperature?[0];
            var windSpeed = forecast.Hourly.WindSpeed10m?[0];
            var windDirection = forecast.Hourly.WindDirection10m?[0];
            var rain = forecast.Hourly.Rain?[0];
            var precipitation = forecast.Hourly.Precipitation?[0];
            var cloudCover = forecast.Hourly.CloudCover?[0];
            var visibility = forecast.Hourly.Visibility?[0];
            var uvIndex = forecast.Hourly.UvIndex?[0];

            result.AppendLine($"Current Conditions (as of {timestamp:g}):");
            result.AppendLine($"  Temperature: {temperature}°C");
            result.AppendLine($"  Apparent Temperature: {apparentTemp}°C");
            result.AppendLine($"  Humidity: {humidity}%");
            result.AppendLine($"  Wind Speed: {windSpeed} km/h");
            result.AppendLine($"  Wind Direction: {windDirection}°");
            result.AppendLine($"  Cloud Cover: {cloudCover}%");
            result.AppendLine($"  Visibility: {visibility}m");
            result.AppendLine($"  Rain: {rain}mm");
            result.AppendLine($"  Precipitation: {precipitation}mm");
            result.AppendLine($"  UV Index: {uvIndex}");
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}