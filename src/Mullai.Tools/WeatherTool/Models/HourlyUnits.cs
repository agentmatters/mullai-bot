using System.Text.Json.Serialization;

namespace Mullai.Tools.WeatherTool.Models;

/// <summary>
/// Represents the units for hourly weather data.
/// </summary>
public class HourlyUnits
{
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public string Temperature2m { get; set; } = string.Empty;

    [JsonPropertyName("relative_humidity_2m")]
    public string RelativeHumidity2m { get; set; } = string.Empty;

    public string Rain { get; set; } = string.Empty;

    public string Snowfall { get; set; } = string.Empty;

    [JsonPropertyName("apparent_temperature")]
    public string ApparentTemperature { get; set; } = string.Empty;

    [JsonPropertyName("dew_point_2m")]
    public string DewPoint2m { get; set; } = string.Empty;

    [JsonPropertyName("precipitation_probability")]
    public string PrecipitationProbability { get; set; } = string.Empty;

    public string Precipitation { get; set; } = string.Empty;

    public string Showers { get; set; } = string.Empty;

    [JsonPropertyName("snow_depth")]
    public string SnowDepth { get; set; } = string.Empty;

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; } = string.Empty;

    [JsonPropertyName("pressure_msl")]
    public string PressureMsl { get; set; } = string.Empty;

    [JsonPropertyName("surface_pressure")]
    public string SurfacePressure { get; set; } = string.Empty;

    [JsonPropertyName("cloud_cover_low")]
    public string CloudCoverLow { get; set; } = string.Empty;

    [JsonPropertyName("cloud_cover")]
    public string CloudCover { get; set; } = string.Empty;

    [JsonPropertyName("cloud_cover_mid")]
    public string CloudCoverMid { get; set; } = string.Empty;

    [JsonPropertyName("cloud_cover_high")]
    public string CloudCoverHigh { get; set; } = string.Empty;

    public string Visibility { get; set; } = string.Empty;

    public string Evapotranspiration { get; set; } = string.Empty;

    [JsonPropertyName("et0_fao_evapotranspiration")]
    public string Et0FaoEvapotranspiration { get; set; } = string.Empty;

    [JsonPropertyName("vapour_pressure_deficit")]
    public string VapourPressureDeficit { get; set; } = string.Empty;

    [JsonPropertyName("temperature_180m")]
    public string Temperature180m { get; set; } = string.Empty;

    [JsonPropertyName("temperature_120m")]
    public string Temperature120m { get; set; } = string.Empty;

    [JsonPropertyName("temperature_80m")]
    public string Temperature80m { get; set; } = string.Empty;

    [JsonPropertyName("wind_gusts_10m")]
    public string WindGusts10m { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_180m")]
    public string WindDirection180m { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_80m")]
    public string WindDirection80m { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_120m")]
    public string WindDirection120m { get; set; } = string.Empty;

    [JsonPropertyName("wind_direction_10m")]
    public string WindDirection10m { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_180m")]
    public string WindSpeed180m { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_120m")]
    public string WindSpeed120m { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_80m")]
    public string WindSpeed80m { get; set; } = string.Empty;

    [JsonPropertyName("wind_speed_10m")]
    public string WindSpeed10m { get; set; } = string.Empty;

    [JsonPropertyName("soil_temperature_0cm")]
    public string SoilTemperature0cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_temperature_6cm")]
    public string SoilTemperature6cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_temperature_18cm")]
    public string SoilTemperature18cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_temperature_54cm")]
    public string SoilTemperature54cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_0_to_1cm")]
    public string SoilMoisture0To1cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_1_to_3cm")]
    public string SoilMoisture1To3cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_9_to_27cm")]
    public string SoilMoisture9To27cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_27_to_81cm")]
    public string SoilMoisture27To81cm { get; set; } = string.Empty;

    [JsonPropertyName("soil_moisture_3_to_9cm")]
    public string SoilMoisture3To9cm { get; set; } = string.Empty;

    [JsonPropertyName("uv_index")]
    public string UvIndex { get; set; } = string.Empty;

    [JsonPropertyName("uv_index_clear_sky")]
    public string UvIndexClearSky { get; set; } = string.Empty;

    [JsonPropertyName("is_day")]
    public string IsDay { get; set; } = string.Empty;

    [JsonPropertyName("sunshine_duration")]
    public string SunshineDuration { get; set; } = string.Empty;

    [JsonPropertyName("wet_bulb_temperature_2m")]
    public string WetBulbTemperature2m { get; set; } = string.Empty;

    [JsonPropertyName("total_column_integrated_water_vapour")]
    public string TotalColumnIntegratedWaterVapour { get; set; } = string.Empty;

    [JsonPropertyName("boundary_layer_height")]
    public string BoundaryLayerHeight { get; set; } = string.Empty;

    [JsonPropertyName("freezing_level_height")]
    public string FreezingLevelHeight { get; set; } = string.Empty;

    [JsonPropertyName("convective_inhibition")]
    public string ConvectiveInhibition { get; set; } = string.Empty;

    [JsonPropertyName("lifted_index")]
    public string LiftedIndex { get; set; } = string.Empty;

    public string Cape { get; set; } = string.Empty;
}
