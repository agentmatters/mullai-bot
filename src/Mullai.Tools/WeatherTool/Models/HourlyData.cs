using System.Text.Json.Serialization;

namespace Mullai.Tools.WeatherTool.Models;

/// <summary>
/// Represents hourly weather data from the Open-Meteo API.
/// </summary>
public class HourlyData
{
    public List<long> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature2m { get; set; } = new();

    [JsonPropertyName("relative_humidity_2m")]
    public List<int?> RelativeHumidity2m { get; set; } = new();

    public List<double?> Rain { get; set; } = new();

    public List<double?> Snowfall { get; set; } = new();

    [JsonPropertyName("apparent_temperature")]
    public List<double?> ApparentTemperature { get; set; } = new();

    [JsonPropertyName("dew_point_2m")]
    public List<double?> DewPoint2m { get; set; } = new();

    [JsonPropertyName("precipitation_probability")]
    public List<int?> PrecipitationProbability { get; set; } = new();

    public List<double?> Precipitation { get; set; } = new();

    public List<double?> Showers { get; set; } = new();

    [JsonPropertyName("snow_depth")]
    public List<double?> SnowDepth { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = new();

    [JsonPropertyName("pressure_msl")]
    public List<double?> PressureMsl { get; set; } = new();

    [JsonPropertyName("surface_pressure")]
    public List<double?> SurfacePressure { get; set; } = new();

    [JsonPropertyName("cloud_cover_low")]
    public List<int?> CloudCoverLow { get; set; } = new();

    [JsonPropertyName("cloud_cover")]
    public List<int?> CloudCover { get; set; } = new();

    [JsonPropertyName("cloud_cover_mid")]
    public List<int?> CloudCoverMid { get; set; } = new();

    [JsonPropertyName("cloud_cover_high")]
    public List<int?> CloudCoverHigh { get; set; } = new();

    public List<double?> Visibility { get; set; } = new();

    public List<double?> Evapotranspiration { get; set; } = new();

    [JsonPropertyName("et0_fao_evapotranspiration")]
    public List<double?> Et0FaoEvapotranspiration { get; set; } = new();

    [JsonPropertyName("vapour_pressure_deficit")]
    public List<double?> VapourPressureDeficit { get; set; } = new();

    [JsonPropertyName("temperature_180m")]
    public List<double?> Temperature180m { get; set; } = new();

    [JsonPropertyName("temperature_120m")]
    public List<double?> Temperature120m { get; set; } = new();

    [JsonPropertyName("temperature_80m")]
    public List<double?> Temperature80m { get; set; } = new();

    [JsonPropertyName("wind_gusts_10m")]
    public List<double?> WindGusts10m { get; set; } = new();

    [JsonPropertyName("wind_direction_180m")]
    public List<int?> WindDirection180m { get; set; } = new();

    [JsonPropertyName("wind_direction_80m")]
    public List<int?> WindDirection80m { get; set; } = new();

    [JsonPropertyName("wind_direction_120m")]
    public List<int?> WindDirection120m { get; set; } = new();

    [JsonPropertyName("wind_direction_10m")]
    public List<int?> WindDirection10m { get; set; } = new();

    [JsonPropertyName("wind_speed_180m")]
    public List<double?> WindSpeed180m { get; set; } = new();

    [JsonPropertyName("wind_speed_120m")]
    public List<double?> WindSpeed120m { get; set; } = new();

    [JsonPropertyName("wind_speed_80m")]
    public List<double?> WindSpeed80m { get; set; } = new();

    [JsonPropertyName("wind_speed_10m")]
    public List<double?> WindSpeed10m { get; set; } = new();

    [JsonPropertyName("soil_temperature_0cm")]
    public List<double?> SoilTemperature0cm { get; set; } = new();

    [JsonPropertyName("soil_temperature_6cm")]
    public List<double?> SoilTemperature6cm { get; set; } = new();

    [JsonPropertyName("soil_temperature_18cm")]
    public List<double?> SoilTemperature18cm { get; set; } = new();

    [JsonPropertyName("soil_temperature_54cm")]
    public List<double?> SoilTemperature54cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_0_to_1cm")]
    public List<double?> SoilMoisture0To1cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_1_to_3cm")]
    public List<double?> SoilMoisture1To3cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_9_to_27cm")]
    public List<double?> SoilMoisture9To27cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_27_to_81cm")]
    public List<double?> SoilMoisture27To81cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_3_to_9cm")]
    public List<double?> SoilMoisture3To9cm { get; set; } = new();

    [JsonPropertyName("uv_index")]
    public List<double?> UvIndex { get; set; } = new();

    [JsonPropertyName("uv_index_clear_sky")]
    public List<double?> UvIndexClearSky { get; set; } = new();

    [JsonPropertyName("is_day")]
    public List<int?> IsDay { get; set; } = new();

    [JsonPropertyName("sunshine_duration")]
    public List<double?> SunshineDuration { get; set; } = new();

    [JsonPropertyName("wet_bulb_temperature_2m")]
    public List<double?> WetBulbTemperature2m { get; set; } = new();

    [JsonPropertyName("total_column_integrated_water_vapour")]
    public List<double?> TotalColumnIntegratedWaterVapour { get; set; } = new();

    [JsonPropertyName("boundary_layer_height")]
    public List<double?> BoundaryLayerHeight { get; set; } = new();

    [JsonPropertyName("freezing_level_height")]
    public List<double?> FreezingLevelHeight { get; set; } = new();

    [JsonPropertyName("convective_inhibition")]
    public List<double?> ConvectiveInhibition { get; set; } = new();

    [JsonPropertyName("lifted_index")]
    public List<double?> LiftedIndex { get; set; } = new();

    public List<double?> Cape { get; set; } = new();
}
