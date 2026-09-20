using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.Services;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Infrastructure.Persistence;
using Xunit;

namespace VarnAI.WeatherSense.UnitTests.Services;

public class ForecastSnapshotTests
{
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider = new();

    private WeatherSenseDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WeatherSenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WeatherSenseDbContext(options);
    }

    [Fact]
    public async Task MultipleForecastSnapshots_ForSameTargetHour_ShouldBothBePreserved()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var targetHourUtc = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        var snapshot1GenTime = new DateTimeOffset(2026, 9, 19, 5, 30, 0, TimeSpan.Zero);
        var snapshot2GenTime = new DateTimeOffset(2026, 9, 19, 11, 30, 0, TimeSpan.Zero);

        var forecastSnapshot1 = new WeatherForecast
        {
            WeatherLocationId = 1,
            Provider = "OpenMeteo",
            ForecastGeneratedAtUtc = snapshot1GenTime,
            ForecastForUtc = targetHourUtc,
            ForecastForLocal = targetHourUtc,
            TemperatureC = 28.0m,
            PrecipitationProbabilityPercent = 20m, // Early morning prediction: 20%
            RainMm = 0m
        };

        var forecastSnapshot2 = new WeatherForecast
        {
            WeatherLocationId = 1,
            Provider = "OpenMeteo",
            ForecastGeneratedAtUtc = snapshot2GenTime,
            ForecastForUtc = targetHourUtc,
            ForecastForLocal = targetHourUtc,
            TemperatureC = 27.2m,
            PrecipitationProbabilityPercent = 80m, // Mid-day updated prediction: 80%
            RainMm = 4.5m
        };

        context.WeatherForecasts.AddRange(forecastSnapshot1, forecastSnapshot2);
        await context.SaveChangesAsync();

        // Act
        var storedSnapshots = await context.WeatherForecasts
            .Where(f => f.WeatherLocationId == 1 && f.ForecastForUtc == targetHourUtc)
            .OrderBy(f => f.ForecastGeneratedAtUtc)
            .ToListAsync();

        // Assert - Both snapshots exist for drift and accuracy analysis
        storedSnapshots.Should().HaveCount(2);
        storedSnapshots[0].ForecastGeneratedAtUtc.Should().Be(snapshot1GenTime);
        storedSnapshots[0].PrecipitationProbabilityPercent.Should().Be(20m);

        storedSnapshots[1].ForecastGeneratedAtUtc.Should().Be(snapshot2GenTime);
        storedSnapshots[1].PrecipitationProbabilityPercent.Should().Be(80m);
    }

    [Fact]
    public async Task GetForecastAsync_ShouldReturnLatestSnapshotWithDailySummary()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(now);

        var location = new WeatherLocation
        {
            Id = 1,
            Name = "Coimbatore Farm",
            Code = "CBE_FARM",
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata"
        };
        context.WeatherLocations.Add(location);

        var snapshotTime = now.AddHours(-1);

        // Add 2 hours for today, 2 hours for tomorrow
        var date1 = new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.FromHours(5.5));
        var date2 = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(5.5));

        var f1 = new WeatherForecast
        {
            WeatherLocationId = 1,
            Provider = "OpenMeteo",
            ForecastGeneratedAtUtc = snapshotTime,
            ForecastForUtc = date1.ToUniversalTime(),
            ForecastForLocal = date1,
            TemperatureC = 30m,
            RainMm = 1.0m,
            WeatherCode = 61,
            WeatherDescription = "Rain: Slight intensity"
        };

        var f2 = new WeatherForecast
        {
            WeatherLocationId = 1,
            Provider = "OpenMeteo",
            ForecastGeneratedAtUtc = snapshotTime,
            ForecastForUtc = date2.ToUniversalTime(),
            ForecastForLocal = date2,
            TemperatureC = 26m,
            RainMm = 5.0m,
            WeatherCode = 65,
            WeatherDescription = "Rain: Heavy intensity"
        };

        context.WeatherForecasts.AddRange(f1, f2);
        await context.SaveChangesAsync();

        var queryService = new WeatherQueryService(context, _mockDateTimeProvider.Object);

        // Act
        var result = await queryService.GetForecastAsync(1, days: 4);

        // Assert
        result.Should().NotBeNull();
        result!.ForecastGeneratedAtUtc.Should().Be(snapshotTime);
        result.Hourly.Should().HaveCount(2);
        result.DailySummary.Should().HaveCount(2);

        var day1Summary = result.DailySummary.First(d => d.DateLocal == DateOnly.FromDateTime(date1.Date));
        day1Summary.TotalRainMm.Should().Be(1.0m);
        day1Summary.DominantWeatherCode.Should().Be(61);

        var day2Summary = result.DailySummary.First(d => d.DateLocal == DateOnly.FromDateTime(date2.Date));
        day2Summary.TotalRainMm.Should().Be(5.0m);
        day2Summary.DominantWeatherCode.Should().Be(65);
    }
}
