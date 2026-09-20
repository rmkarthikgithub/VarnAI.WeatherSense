using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.Common.Models;
using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.Services;
using VarnAI.WeatherSense.Application.Validators;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Domain.Enums;
using VarnAI.WeatherSense.Infrastructure.Persistence;
using Xunit;

namespace VarnAI.WeatherSense.UnitTests.Services;

public class WeatherCollectionOrchestrationTests
{
    private readonly Mock<IWeatherProvider> _mockWeatherProvider = new();
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider = new();
    private readonly Mock<ILogger<WeatherCollectionService>> _mockLogger = new();
    private readonly CollectWeatherRequestValidator _validator = new();

    private WeatherSenseDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WeatherSenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WeatherSenseDbContext(options);
    }

    [Fact]
    public async Task CollectAllAsync_WhenOneLocationFails_ShouldContinueAndCompleteOtherLocations()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = new DateTimeOffset(2026, 9, 19, 6, 0, 0, TimeSpan.Zero);
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(now);
        _mockWeatherProvider.Setup(p => p.ProviderName).Returns("OpenMeteo");

        var location1 = new WeatherLocation
        {
            Id = 1,
            Name = "Location Succeeded",
            Code = "LOC_SUCCESS",
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata",
            IsActive = true,
            CollectionEnabled = true
        };

        var location2 = new WeatherLocation
        {
            Id = 2,
            Name = "Location Failed",
            Code = "LOC_FAIL",
            Latitude = 12.0m,
            Longitude = 78.0m,
            Timezone = "Asia/Kolkata",
            IsActive = true,
            CollectionEnabled = true
        };

        context.WeatherLocations.AddRange(location1, location2);
        await context.SaveChangesAsync();

        // Location 1 provider succeeds
        var hourlyPoint = new WeatherPointDto
        {
            TimeUtc = now.AddHours(-1),
            TimeLocal = now.AddHours(-1),
            TemperatureC = 25m,
            RainMm = 0m
        };
        var successResult = new WeatherProviderResult
        {
            Success = true,
            GeneratedAtUtc = now,
            Provider = "OpenMeteo",
            Hourly = new List<WeatherPointDto> { hourlyPoint }
        };

        _mockWeatherProvider
            .Setup(p => p.GetForecastAsync(It.Is<WeatherLocation>(l => l.Id == 1), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Location 2 provider fails
        var failResult = WeatherProviderResult.Fail("Provider rate limit reached (HTTP 429)", 429, null, "OpenMeteo");
        _mockWeatherProvider
            .Setup(p => p.GetForecastAsync(It.Is<WeatherLocation>(l => l.Id == 2), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        var service = new WeatherCollectionService(
            context,
            _mockWeatherProvider.Object,
            _validator,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);

        var request = new CollectWeatherRequest { ForecastDays = 4 };

        // Act
        var response = await service.CollectAllAsync(request, CollectionTriggerSource.N8n);

        // Assert
        response.Should().NotBeNull();
        response.LocationsProcessed.Should().Be(2);
        response.LocationsSucceeded.Should().Be(1);
        response.LocationsFailed.Should().Be(1);
        response.Status.Should().Be(CollectionExecutionStatus.CompletedWithErrors.ToString());

        // Check DB state
        var executionInDb = await context.WeatherCollectionExecutions
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.Id == response.ExecutionId);

        executionInDb.Should().NotBeNull();
        executionInDb!.Status.Should().Be(CollectionExecutionStatus.CompletedWithErrors);
        executionInDb.Details.Should().HaveCount(2);

        var detail1 = executionInDb.Details.First(d => d.WeatherLocationId == 1);
        detail1.Status.Should().Be("Completed");

        var detail2 = executionInDb.Details.First(d => d.WeatherLocationId == 2);
        detail2.Status.Should().Be("Failed");
        detail2.ErrorMessage.Should().Contain("429");
    }

    [Fact]
    public async Task CollectAllAsync_WhenRunningRepeatedly_ShouldSkipDuplicateObservations()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = new DateTimeOffset(2026, 9, 19, 6, 0, 0, TimeSpan.Zero);
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(now);
        _mockWeatherProvider.Setup(p => p.ProviderName).Returns("OpenMeteo");

        var location = new WeatherLocation
        {
            Id = 1,
            Name = "Single Farm",
            Code = "SINGLE_FARM",
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata",
            IsActive = true,
            CollectionEnabled = true
        };
        context.WeatherLocations.Add(location);
        await context.SaveChangesAsync();

        var point = new WeatherPointDto
        {
            TimeUtc = now.AddHours(-1),
            TimeLocal = now.AddHours(-1),
            TemperatureC = 25m,
            RainMm = 0m
        };
        var providerResult = new WeatherProviderResult
        {
            Success = true,
            GeneratedAtUtc = now,
            Provider = "OpenMeteo",
            Hourly = new List<WeatherPointDto> { point }
        };

        _mockWeatherProvider
            .Setup(p => p.GetForecastAsync(It.IsAny<WeatherLocation>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResult);

        var service = new WeatherCollectionService(
            context,
            _mockWeatherProvider.Object,
            _validator,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);

        var request = new CollectWeatherRequest { ForecastDays = 4 };

        // Act - Run 1
        var response1 = await service.CollectAllAsync(request, CollectionTriggerSource.N8n);

        // Act - Run 2 (immediate retry or next run with overlapping observation point)
        var response2 = await service.CollectAllAsync(request, CollectionTriggerSource.N8n);

        // Assert
        response1.RecordsInserted.Should().BeGreaterThan(0);

        // In Run 2, the historical observation for that hour is already in the database and skipped
        var locationResult2 = response2.Locations.First();
        locationResult2.RecordsSkipped.Should().BeGreaterThan(0);

        // Total observations in DB should not have duplicate entries for that hour
        var obsCount = await context.WeatherObservations
            .CountAsync(o => o.WeatherLocationId == 1 && o.ObservedAtUtc == point.TimeUtc);

        obsCount.Should().Be(1);
    }
}
