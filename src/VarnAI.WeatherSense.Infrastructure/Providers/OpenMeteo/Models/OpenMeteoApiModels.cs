using System.Text.Json.Serialization;

namespace VarnAI.WeatherSense.Infrastructure.Providers.OpenMeteo.Models;

public class OpenMeteoForecastApiResponse
{
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("timezone_abbreviation")]
    public string? TimezoneAbbreviation { get; set; }

    [JsonPropertyName("elevation")]
    public double? Elevation { get; set; }

    [JsonPropertyName("current")]
    public OpenMeteoCurrentBlock? Current { get; set; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourlyBlock? Hourly { get; set; }
}

public class OpenMeteoCurrentBlock
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public decimal? Temperature2m { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public decimal? RelativeHumidity2m { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public decimal? ApparentTemperature { get; set; }

    [JsonPropertyName("precipitation")]
    public decimal? Precipitation { get; set; }

    [JsonPropertyName("rain")]
    public decimal? Rain { get; set; }

    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; set; }

    [JsonPropertyName("pressure_msl")]
    public decimal? PressureMsl { get; set; }

    [JsonPropertyName("cloud_cover")]
    public decimal? CloudCover { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public decimal? WindSpeed10m { get; set; }

    [JsonPropertyName("wind_direction_10m")]
    public decimal? WindDirection10m { get; set; }

    [JsonPropertyName("wind_gusts_10m")]
    public decimal? WindGusts10m { get; set; }
}

public class OpenMeteoHourlyBlock
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<decimal?> Temperature2m { get; set; } = new();

    [JsonPropertyName("relative_humidity_2m")]
    public List<decimal?> RelativeHumidity2m { get; set; } = new();

    [JsonPropertyName("apparent_temperature")]
    public List<decimal?> ApparentTemperature { get; set; } = new();

    [JsonPropertyName("precipitation_probability")]
    public List<decimal?> PrecipitationProbability { get; set; } = new();

    [JsonPropertyName("precipitation")]
    public List<decimal?> Precipitation { get; set; } = new();

    [JsonPropertyName("rain")]
    public List<decimal?> Rain { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = new();

    [JsonPropertyName("pressure_msl")]
    public List<decimal?> PressureMsl { get; set; } = new();

    [JsonPropertyName("cloud_cover")]
    public List<decimal?> CloudCover { get; set; } = new();

    [JsonPropertyName("wind_speed_10m")]
    public List<decimal?> WindSpeed10m { get; set; } = new();

    [JsonPropertyName("wind_direction_10m")]
    public List<decimal?> WindDirection10m { get; set; } = new();

    [JsonPropertyName("wind_gusts_10m")]
    public List<decimal?> WindGusts10m { get; set; } = new();

    [JsonPropertyName("shortwave_radiation")]
    public List<decimal?> ShortwaveRadiation { get; set; } = new();

    [JsonPropertyName("et0_fao_evapotranspiration")]
    public List<decimal?> Et0FaoEvapotranspiration { get; set; } = new();

    [JsonPropertyName("soil_temperature_0_to_7cm")]
    public List<decimal?> SoilTemperature0To7cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_0_to_7cm")]
    public List<decimal?> SoilMoisture0To7cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_7_to_28cm")]
    public List<decimal?> SoilMoisture7To28cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_28_to_100cm")]
    public List<decimal?> SoilMoisture28To100cm { get; set; } = new();

    [JsonPropertyName("soil_moisture_100_to_255cm")]
    public List<decimal?> SoilMoisture100To255cm { get; set; } = new();
}
