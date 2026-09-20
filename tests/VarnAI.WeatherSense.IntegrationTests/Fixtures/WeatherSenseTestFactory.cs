using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.Common.Models;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Infrastructure.Persistence;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace VarnAI.WeatherSense.IntegrationTests.Fixtures;

public class WeatherSenseTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IntegrationTestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["WeatherSenseSecurity:ApiKey"] = "test-api-key",
                ["ConnectionStrings:WeatherSenseDb"] = "InMemory:" + _dbName
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Replace IWeatherProvider with mock stub
            services.RemoveAll<IWeatherProvider>();
            services.AddScoped<IWeatherProvider, StubWeatherProvider>();
        });
    }
}

public class StubWeatherProvider : IWeatherProvider
{
    public string ProviderName => "OpenMeteo";

    public Task<WeatherProviderResult> GetCurrentAsync(WeatherLocation location, CancellationToken cancellationToken = default)
    {
        return GetForecastAsync(location, 1, cancellationToken);
    }

    public Task<WeatherProviderResult> GetForecastAsync(WeatherLocation location, int forecastDays, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var current = new WeatherPointDto
        {
            TimeUtc = now,
            TimeLocal = now.ToOffset(TimeSpan.FromHours(5.5)),
            TemperatureC = 26.5m,
            FeelsLikeTemperatureC = 28.0m,
            RelativeHumidityPercent = 75m,
            PrecipitationMm = 0m,
            RainMm = 0m,
            WindSpeedKmh = 12.0m,
            WindDirectionDegrees = 200m,
            PressureHpa = 1012.0m,
            CloudCoverPercent = 40m,
            WeatherCode = 2,
            WeatherDescription = "Partly cloudy"
        };

        var hourly = new List<WeatherPointDto>();
        for (int i = -2; i < forecastDays * 24; i++)
        {
            var pointTime = new DateTimeOffset(now.Date.AddHours(i), TimeSpan.Zero);
            hourly.Add(new WeatherPointDto
            {
                TimeUtc = pointTime,
                TimeLocal = pointTime.ToOffset(TimeSpan.FromHours(5.5)),
                TemperatureC = 22.0m + (i % 8),
                FeelsLikeTemperatureC = 24.0m + (i % 8),
                RelativeHumidityPercent = 65m + (i % 20),
                PrecipitationMm = i % 5 == 0 ? 1.5m : 0m,
                RainMm = i % 5 == 0 ? 1.5m : 0m,
                PrecipitationProbabilityPercent = i % 5 == 0 ? 60m : 10m,
                WindSpeedKmh = 10.0m + (i % 5),
                WindDirectionDegrees = 180m,
                PressureHpa = 1010.0m,
                CloudCoverPercent = 50m,
                SolarRadiationWm2 = 200m,
                Et0Mm = 0.25m,
                SoilMoisture0To7Cm = 0.1800m,
                SoilMoisture7To28Cm = 0.1700m,
                SoilTemperatureC = 25.0m,
                WeatherCode = i % 5 == 0 ? 61 : 2,
                WeatherDescription = i % 5 == 0 ? "Rain: Slight intensity" : "Partly cloudy"
            });
        }

        return Task.FromResult(new WeatherProviderResult
        {
            Success = true,
            GeneratedAtUtc = now,
            Provider = ProviderName,
            Model = "OpenMeteo-Stub",
            Current = current,
            Hourly = hourly,
            RawJson = "{\"stub\": true}"
        });
    }

    public Task<WeatherProviderResult> GetHistoricalAsync(WeatherLocation location, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return GetForecastAsync(location, 1, cancellationToken);
    }
}
