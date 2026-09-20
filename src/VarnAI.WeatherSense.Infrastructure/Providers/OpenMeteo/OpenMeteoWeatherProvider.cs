using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.Common.Models;
using VarnAI.WeatherSense.Application.Mappings;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Infrastructure.Options;
using VarnAI.WeatherSense.Infrastructure.Providers.OpenMeteo.Models;

namespace VarnAI.WeatherSense.Infrastructure.Providers.OpenMeteo;

public class OpenMeteoWeatherProvider : IWeatherProvider
{
    public const string Provider = "OpenMeteo";
    public string ProviderName => Provider;

    private readonly HttpClient _httpClient;
    private readonly OpenMeteoOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<OpenMeteoWeatherProvider> _logger;

    private const string HourlyVariables =
        "temperature_2m,relative_humidity_2m,apparent_temperature,precipitation_probability,precipitation,rain,weather_code,pressure_msl,cloud_cover,wind_speed_10m,wind_direction_10m,wind_gusts_10m,shortwave_radiation,et0_fao_evapotranspiration,soil_temperature_0_to_7cm,soil_moisture_0_to_7cm,soil_moisture_7_to_28cm,soil_moisture_28_to_100cm,soil_moisture_100_to_255cm";

    private const string CurrentVariables =
        "temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,weather_code,pressure_msl,cloud_cover,wind_speed_10m,wind_direction_10m,wind_gusts_10m";

    public OpenMeteoWeatherProvider(
        HttpClient httpClient,
        IOptions<OpenMeteoOptions> options,
        IDateTimeProvider dateTimeProvider,
        ILogger<OpenMeteoWeatherProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<WeatherProviderResult> GetCurrentAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default)
    {
        return await GetForecastAsync(location, forecastDays: 1, cancellationToken);
    }

    public async Task<WeatherProviderResult> GetForecastAsync(
        WeatherLocation location,
        int forecastDays,
        CancellationToken cancellationToken = default)
    {
        var clampedDays = Math.Clamp(forecastDays, 1, _options.MaxForecastDays);
        var timezoneParam = Uri.EscapeDataString(string.IsNullOrWhiteSpace(location.Timezone) ? "Asia/Kolkata" : location.Timezone);

        // past_days=1 retrieves past 24 hours of observations alongside the forecast
        var url = $"v1/forecast?latitude={location.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={location.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&hourly={HourlyVariables}" +
                  $"&current={CurrentVariables}" +
                  $"&past_days=1" +
                  $"&forecast_days={clampedDays}" +
                  $"&timezone={timezoneParam}";

        return await ExecuteWeatherApiCallAsync(url, location, cancellationToken);
    }

    public async Task<WeatherProviderResult> GetHistoricalAsync(
        WeatherLocation location,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var timezoneParam = Uri.EscapeDataString(string.IsNullOrWhiteSpace(location.Timezone) ? "Asia/Kolkata" : location.Timezone);
        var url = $"https://archive-api.open-meteo.com/v1/archive" +
                  $"?latitude={location.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={location.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&start_date={startDate:yyyy-MM-dd}" +
                  $"&end_date={endDate:yyyy-MM-dd}" +
                  $"&hourly={HourlyVariables}" +
                  $"&timezone={timezoneParam}";

        return await ExecuteWeatherApiCallAsync(url, location, cancellationToken);
    }

    private async Task<WeatherProviderResult> ExecuteWeatherApiCallAsync(
        string url,
        WeatherLocation location,
        CancellationToken cancellationToken)
    {
        var requestTime = _dateTimeProvider.UtcNow;
        try
        {
            _logger.LogInformation("Calling Open-Meteo API for location {LocationCode} ({Latitude}, {Longitude})",
                location.Code, location.Latitude, location.Longitude);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo returned HTTP {StatusCode} for location {LocationCode}. Body: {Body}",
                    response.StatusCode, location.Code, rawJson);

                return WeatherProviderResult.Fail(
                    $"Open-Meteo API returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    (int)response.StatusCode,
                    rawJson,
                    ProviderName);
            }

            var apiResponse = JsonSerializer.Deserialize<OpenMeteoForecastApiResponse>(rawJson);
            if (apiResponse == null || apiResponse.Hourly == null)
            {
                return WeatherProviderResult.Fail(
                    "Open-Meteo response could not be deserialized or was missing hourly block.",
                    (int)response.StatusCode,
                    rawJson,
                    ProviderName);
            }

            var timeZoneOffset = TimeSpan.FromSeconds(apiResponse.UtcOffsetSeconds);

            // Map Current
            WeatherPointDto? currentDto = null;
            if (apiResponse.Current != null && !string.IsNullOrWhiteSpace(apiResponse.Current.Time))
            {
                currentDto = MapCurrentPoint(apiResponse.Current, timeZoneOffset);
            }

            // Map Hourly
            var hourlyDtos = MapHourlyPoints(apiResponse.Hourly, timeZoneOffset);

            return new WeatherProviderResult
            {
                Success = true,
                HttpStatusCode = (int)response.StatusCode,
                RawJson = rawJson,
                Provider = ProviderName,
                Model = "OpenMeteo-BestMatch",
                GeneratedAtUtc = requestTime,
                Current = currentDto,
                Hourly = hourlyDtos
            };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogInformation("Open-Meteo request was cancelled for location {LocationCode}", location.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception querying Open-Meteo for location {LocationCode}: {Message}", location.Code, ex.Message);
            return WeatherProviderResult.Fail(
                $"Failed to connect to Open-Meteo: {ex.Message}",
                null,
                null,
                ProviderName);
        }
    }

    private static WeatherPointDto MapCurrentPoint(OpenMeteoCurrentBlock c, TimeSpan offset)
    {
        var localTime = DateTime.Parse(c.Time, CultureInfo.InvariantCulture);
        var timeLocal = new DateTimeOffset(localTime, offset);
        var timeUtc = timeLocal.ToUniversalTime();

        return new WeatherPointDto
        {
            TimeUtc = timeUtc,
            TimeLocal = timeLocal,
            TemperatureC = c.Temperature2m,
            FeelsLikeTemperatureC = c.ApparentTemperature,
            RelativeHumidityPercent = c.RelativeHumidity2m,
            PrecipitationMm = c.Precipitation,
            RainMm = c.Rain,
            WindSpeedKmh = c.WindSpeed10m,
            WindDirectionDegrees = c.WindDirection10m,
            WindGustKmh = c.WindGusts10m,
            PressureHpa = c.PressureMsl,
            CloudCoverPercent = c.CloudCover,
            WeatherCode = c.WeatherCode,
            WeatherDescription = WeatherCodeMapper.ToDescription(c.WeatherCode)
        };
    }

    private static List<WeatherPointDto> MapHourlyPoints(OpenMeteoHourlyBlock h, TimeSpan offset)
    {
        var points = new List<WeatherPointDto>();
        var count = h.Time.Count;

        for (int i = 0; i < count; i++)
        {
            var rawTimeStr = h.Time[i];
            var localTime = DateTime.Parse(rawTimeStr, CultureInfo.InvariantCulture);
            var timeLocal = new DateTimeOffset(localTime, offset);
            var timeUtc = timeLocal.ToUniversalTime();

            int? code = i < h.WeatherCode.Count ? h.WeatherCode[i] : null;

            points.Add(new WeatherPointDto
            {
                TimeUtc = timeUtc,
                TimeLocal = timeLocal,
                TemperatureC = i < h.Temperature2m.Count ? h.Temperature2m[i] : null,
                FeelsLikeTemperatureC = i < h.ApparentTemperature.Count ? h.ApparentTemperature[i] : null,
                RelativeHumidityPercent = i < h.RelativeHumidity2m.Count ? h.RelativeHumidity2m[i] : null,
                PrecipitationProbabilityPercent = i < h.PrecipitationProbability.Count ? h.PrecipitationProbability[i] : null,
                PrecipitationMm = i < h.Precipitation.Count ? h.Precipitation[i] : null,
                RainMm = i < h.Rain.Count ? h.Rain[i] : null,
                WindSpeedKmh = i < h.WindSpeed10m.Count ? h.WindSpeed10m[i] : null,
                WindDirectionDegrees = i < h.WindDirection10m.Count ? h.WindDirection10m[i] : null,
                WindGustKmh = i < h.WindGusts10m.Count ? h.WindGusts10m[i] : null,
                CloudCoverPercent = i < h.CloudCover.Count ? h.CloudCover[i] : null,
                PressureHpa = i < h.PressureMsl.Count ? h.PressureMsl[i] : null,
                SolarRadiationWm2 = i < h.ShortwaveRadiation.Count ? h.ShortwaveRadiation[i] : null,
                Et0Mm = i < h.Et0FaoEvapotranspiration.Count ? h.Et0FaoEvapotranspiration[i] : null,
                SoilTemperatureC = i < h.SoilTemperature0To7cm.Count ? h.SoilTemperature0To7cm[i] : null,
                SoilMoisture0To7Cm = i < h.SoilMoisture0To7cm.Count ? h.SoilMoisture0To7cm[i] : null,
                SoilMoisture7To28Cm = i < h.SoilMoisture7To28cm.Count ? h.SoilMoisture7To28cm[i] : null,
                SoilMoisture28To100Cm = i < h.SoilMoisture28To100cm.Count ? h.SoilMoisture28To100cm[i] : null,
                SoilMoisture100To255Cm = i < h.SoilMoisture100To255cm.Count ? h.SoilMoisture100To255cm[i] : null,
                WeatherCode = code,
                WeatherDescription = WeatherCodeMapper.ToDescription(code)
            });
        }

        return points;
    }
}
