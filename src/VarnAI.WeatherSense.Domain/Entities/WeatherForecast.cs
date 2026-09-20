using VarnAI.WeatherSense.Domain.Common;

namespace VarnAI.WeatherSense.Domain.Entities;

public class WeatherForecast : BaseEntity<long>
{
    public int WeatherLocationId { get; set; }
    public DateTimeOffset ForecastGeneratedAtUtc { get; set; }
    public DateTimeOffset ForecastForUtc { get; set; }
    public DateTimeOffset ForecastForLocal { get; set; }

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

    public string Provider { get; set; } = "OpenMeteo";
    public string? ProviderModel { get; set; }

    // Navigation
    public WeatherLocation Location { get; set; } = null!;
}
