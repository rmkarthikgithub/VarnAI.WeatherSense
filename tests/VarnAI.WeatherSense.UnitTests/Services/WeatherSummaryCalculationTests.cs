using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.Services;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Infrastructure.Persistence;
using Xunit;

namespace VarnAI.WeatherSense.UnitTests.Services;

public class WeatherSummaryCalculationTests
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
    public async Task GetSummaryAsync_WithObservations_ShouldComputeAccurateAggregations()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.FromHours(5.5));
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(now);

        var location = new WeatherLocation
        {
            Id = 1,
            Name = "Emmampoondi",
            Code = "EMMAMPOONDI",
            Latitude = 11.23m,
            Longitude = 77.12m,
            Timezone = "Asia/Kolkata"
        };
        context.WeatherLocations.Add(location);

        // Add 3 observation hours:
        // Hour 1: Temp 20C, Humidity 80%, Rain 0mm, Wind 10kmh, Soil 0.15, ET0 0.1
        // Hour 2: Temp 25C, Humidity 70%, Rain 2.5mm, Wind 15kmh, Soil 0.18, ET0 0.3
        // Hour 3: Temp 30C, Humidity 60%, Rain 5.0mm, Wind 20kmh, Soil 0.22, ET0 0.5
        var obs1 = new WeatherObservation
        {
            WeatherLocationId = 1,
            ObservedAtUtc = now.AddHours(-3),
            ObservedAtLocal = now.AddHours(-3),
            TemperatureC = 20m,
            RelativeHumidityPercent = 80m,
            RainMm = 0m,
            WindSpeedKmh = 10m,
            SoilMoisture0To7Cm = 0.1500m,
            Et0Mm = 0.10m
        };
        var obs2 = new WeatherObservation
        {
            WeatherLocationId = 1,
            ObservedAtUtc = now.AddHours(-2),
            ObservedAtLocal = now.AddHours(-2),
            TemperatureC = 25m,
            RelativeHumidityPercent = 70m,
            RainMm = 2.5m,
            WindSpeedKmh = 15m,
            SoilMoisture0To7Cm = 0.1800m,
            Et0Mm = 0.30m
        };
        var obs3 = new WeatherObservation
        {
            WeatherLocationId = 1,
            ObservedAtUtc = now.AddHours(-1),
            ObservedAtLocal = now.AddHours(-1),
            TemperatureC = 30m,
            RelativeHumidityPercent = 60m,
            RainMm = 5.0m,
            WindSpeedKmh = 20m,
            SoilMoisture0To7Cm = 0.2200m,
            Et0Mm = 0.50m
        };

        context.WeatherObservations.AddRange(obs1, obs2, obs3);
        await context.SaveChangesAsync();

        var queryService = new WeatherQueryService(context, _mockDateTimeProvider.Object);

        // Act
        var summary = await queryService.GetSummaryAsync(1, now.AddHours(-4), now);

        // Assert
        summary.Should().NotBeNull();
        summary!.MinTemperatureC.Should().Be(20m);
        summary.MaxTemperatureC.Should().Be(30m);
        summary.AvgTemperatureC.Should().Be(25m);
        summary.AvgHumidityPercent.Should().Be(70m);
        summary.TotalRainfallMm.Should().Be(7.5m);
        summary.MaxHourlyRainfallMm.Should().Be(5.0m);
        summary.AvgWindSpeedKmh.Should().Be(15m);
        summary.MaxWindSpeedKmh.Should().Be(20m);
        summary.AvgSoilMoisture0To7Cm.Should().Be(0.1833m);
        summary.MinSoilMoisture0To7Cm.Should().Be(0.1500m);
        summary.MaxSoilMoisture0To7Cm.Should().Be(0.2200m);
        summary.TotalEt0Mm.Should().Be(0.90m);
        summary.RainyHoursCount.Should().Be(2); // Hour 2 and Hour 3 had rain > 0
        summary.TotalHoursCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoData_ShouldReturnZeroCountsWithoutThrowing()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = DateTimeOffset.UtcNow;
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(now);

        var location = new WeatherLocation
        {
            Id = 2,
            Name = "Empty Farm",
            Code = "EMPTY_FARM",
            Latitude = 10m,
            Longitude = 70m,
            Timezone = "Asia/Kolkata"
        };
        context.WeatherLocations.Add(location);
        await context.SaveChangesAsync();

        var queryService = new WeatherQueryService(context, _mockDateTimeProvider.Object);

        // Act
        var summary = await queryService.GetSummaryAsync(2, now.AddDays(-7), now);

        // Assert
        summary.Should().NotBeNull();
        summary!.TotalHoursCount.Should().Be(0);
        summary.RainyHoursCount.Should().Be(0);
        summary.AvgTemperatureC.Should().BeNull();
    }
}
