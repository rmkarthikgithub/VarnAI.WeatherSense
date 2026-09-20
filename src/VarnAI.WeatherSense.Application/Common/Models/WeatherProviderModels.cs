namespace VarnAI.WeatherSense.Application.Common.Models;

public class WeatherProviderResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? RawJson { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public WeatherPointDto? Current { get; set; }
    public IReadOnlyList<WeatherPointDto> Hourly { get; set; } = Array.Empty<WeatherPointDto>();

    public static WeatherProviderResult Fail(string error, int? httpStatusCode = null, string? rawJson = null, string provider = "")
    {
        return new WeatherProviderResult
        {
            Success = false,
            Error = error,
            HttpStatusCode = httpStatusCode,
            RawJson = rawJson,
            Provider = provider
        };
    }
}

public class WeatherPointDto
{
    public DateTimeOffset TimeUtc { get; set; }
    public DateTimeOffset TimeLocal { get; set; }

    public decimal? TemperatureC { get; set; }
    public decimal? FeelsLikeTemperatureC { get; set; }
    public decimal? RelativeHumidityPercent { get; set; }
    public decimal? PrecipitationMm { get; set; }
    public decimal? RainMm { get; set; }
    public decimal? PrecipitationProbabilityPercent { get; set; }
    public decimal? WindSpeedKmh { get; set; }
    public decimal? WindDirectionDegrees { get; set; }
    public decimal? WindGustKmh { get; set; }
    public decimal? CloudCoverPercent { get; set; }
    public decimal? PressureHpa { get; set; }
    public decimal? SolarRadiationWm2 { get; set; }
    public decimal? Et0Mm { get; set; }

    public decimal? SoilMoisture0To7Cm { get; set; }
    public decimal? SoilMoisture7To28Cm { get; set; }
    public decimal? SoilMoisture28To100Cm { get; set; }
    public decimal? SoilMoisture100To255Cm { get; set; }
    public decimal? SoilTemperatureC { get; set; }

    public int? WeatherCode { get; set; }
    public string? WeatherDescription { get; set; }
}
