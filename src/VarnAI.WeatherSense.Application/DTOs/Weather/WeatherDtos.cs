using VarnAI.WeatherSense.Application.DTOs.Locations;

namespace VarnAI.WeatherSense.Application.DTOs.Weather;

public class CurrentWeatherResponse
{
    public WeatherLocationHeaderDto Location { get; set; } = null!;
    public CurrentWeatherPointDto Weather { get; set; } = null!;
}

public class CurrentWeatherPointDto
{
    public DateTimeOffset ObservedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? FeelsLikeTemperatureC { get; set; }
    public decimal? HumidityPercent { get; set; }
    public decimal? RainMm { get; set; }
    public decimal? PrecipitationMm { get; set; }
    public decimal? WindSpeedKmh { get; set; }
    public decimal? WindDirectionDegrees { get; set; }
    public decimal? WindGustKmh { get; set; }
    public decimal? PressureHpa { get; set; }
    public decimal? CloudCoverPercent { get; set; }
    public decimal? SolarRadiationWm2 { get; set; }
    public decimal? Et0Mm { get; set; }
    public decimal? SoilMoisture0To7Cm { get; set; }
    public decimal? SoilMoisture7To28Cm { get; set; }
    public decimal? SoilTemperatureC { get; set; }
    public int? WeatherCode { get; set; }
    public string? WeatherDescription { get; set; }
    public string DataSource { get; set; } = string.Empty;
}

public class HourlyWeatherDto
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

public class ForecastResponseDto
{
    public WeatherLocationHeaderDto Location { get; set; } = null!;
    public DateTimeOffset ForecastGeneratedAtUtc { get; set; }
    public int Days { get; set; }
    public List<HourlyWeatherDto> Hourly { get; set; } = new();
    public List<DailyForecastSummaryDto> DailySummary { get; set; } = new();
}

public class DailyForecastSummaryDto
{
    public DateOnly DateLocal { get; set; }
    public decimal? MinTemperatureC { get; set; }
    public decimal? MaxTemperatureC { get; set; }
    public decimal? AvgHumidityPercent { get; set; }
    public decimal? TotalRainMm { get; set; }
    public decimal? MaxRainProbabilityPercent { get; set; }
    public decimal? MaxWindSpeedKmh { get; set; }
    public decimal? TotalEt0Mm { get; set; }
    public decimal? AvgSoilMoisture0To7Cm { get; set; }
    public int? DominantWeatherCode { get; set; }
    public string? DominantWeatherDescription { get; set; }
}

public class WeatherHistoryResponse
{
    public WeatherLocationHeaderDto Location { get; set; } = null!;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<HourlyWeatherDto> Records { get; set; } = new();
}

public class WeatherSummaryDto
{
    public WeatherLocationHeaderDto Location { get; set; } = null!;
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public decimal? MinTemperatureC { get; set; }
    public decimal? MaxTemperatureC { get; set; }
    public decimal? AvgTemperatureC { get; set; }
    public decimal? AvgHumidityPercent { get; set; }
    public decimal? TotalRainfallMm { get; set; }
    public decimal? MaxHourlyRainfallMm { get; set; }
    public decimal? AvgWindSpeedKmh { get; set; }
    public decimal? MaxWindSpeedKmh { get; set; }
    public decimal? AvgSoilMoisture0To7Cm { get; set; }
    public decimal? MinSoilMoisture0To7Cm { get; set; }
    public decimal? MaxSoilMoisture0To7Cm { get; set; }
    public decimal? TotalEt0Mm { get; set; }
    public int RainyHoursCount { get; set; }
    public int TotalHoursCount { get; set; }
}

public class WeatherDatasetDto
{
    public WeatherLocationHeaderDto Location { get; set; } = null!;
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public string Interval { get; set; } = "hourly";
    public int RowCount { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<WeatherDatasetRowDto> Rows { get; set; } = new();
}

public class WeatherDatasetRowDto
{
    public DateTimeOffset TimestampUtc { get; set; }
    public DateTimeOffset TimestampLocal { get; set; }
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
