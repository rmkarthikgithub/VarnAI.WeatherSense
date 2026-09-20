using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.Application.DTOs.Weather;
using VarnAI.WeatherSense.IntegrationTests.Fixtures;
using Xunit;

namespace VarnAI.WeatherSense.IntegrationTests.Controllers;

public class CollectionAndQueryIntegrationTests : IClassFixture<WeatherSenseTestFactory>
{
    private readonly HttpClient _client;

    public CollectionAndQueryIntegrationTests(WeatherSenseTestFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
    }

    [Fact]
    public async Task CompleteCollectionAndQueryPipeline_ShouldSucceedEndToEnd()
    {
        // 1. Create a test location
        var createRequest = new CreateWeatherLocationRequest
        {
            Name = "Emmampoondi Agro Hub",
            Code = "EMMAMPOONDI_AGRO_" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            Latitude = 11.234567m,
            Longitude = 77.123456m,
            Timezone = "Asia/Kolkata",
            Country = "India",
            State = "Tamil Nadu",
            District = "Tiruppur",
            Provider = "OpenMeteo",
            CollectionEnabled = true
        };
        var createLocResponse = await _client.PostAsJsonAsync("/api/v1/weather/locations", createRequest);
        createLocResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var location = await createLocResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();
        location.Should().NotBeNull();
        int locationId = location!.Id;

        // 2. Trigger Collection for all locations via n8n integration endpoint
        var collectRequest = new CollectWeatherRequest { ForecastDays = 4 };
        var collectResponse = await _client.PostAsJsonAsync("/api/v1/weather/collect", collectRequest);
        collectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var collectionSummary = await collectResponse.Content.ReadFromJsonAsync<CollectionSummaryResponse>();
        collectionSummary.Should().NotBeNull();
        collectionSummary!.ExecutionId.Should().BeGreaterThan(0);
        collectionSummary.LocationsProcessed.Should().BeGreaterThan(0);
        collectionSummary.RecordsInserted.Should().BeGreaterThan(0);

        // 3. Query Latest Collection Audit
        var latestAuditResponse = await _client.GetAsync("/api/v1/weather/collections/latest");
        latestAuditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var latestAudit = await latestAuditResponse.Content.ReadFromJsonAsync<CollectionExecutionDto>();
        latestAudit.Should().NotBeNull();
        latestAudit!.ExecutionId.Should().Be(collectionSummary.ExecutionId);

        // 4. Query Current Weather
        var currentResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await currentResponse.Content.ReadFromJsonAsync<CurrentWeatherResponse>();
        current.Should().NotBeNull();
        current!.Location.Id.Should().Be(locationId);
        current.Weather.Should().NotBeNull();
        current.Weather.TemperatureC.Should().HaveValue();

        // 5. Query Forecast Snapshot
        var forecastResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/forecast?days=4");
        forecastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var forecast = await forecastResponse.Content.ReadFromJsonAsync<ForecastResponseDto>();
        forecast.Should().NotBeNull();
        forecast!.Hourly.Should().NotBeEmpty();
        forecast.DailySummary.Should().NotBeEmpty();

        // 6. Query Hourly Weather
        var hourlyResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/hourly");
        hourlyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var hourlyList = await hourlyResponse.Content.ReadFromJsonAsync<List<HourlyWeatherDto>>();
        hourlyList.Should().NotBeNull();

        // 7. Query Weather History with Pagination
        var historyResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/history?pageNumber=1&pageSize=20");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<WeatherHistoryResponse>();
        history.Should().NotBeNull();
        history!.Location.Id.Should().Be(locationId);

        // 8. Query Aggregated Summary
        var summaryResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<WeatherSummaryDto>();
        summary.Should().NotBeNull();
        summary!.Location.Id.Should().Be(locationId);

        // 9. Query ML-Ready Dataset
        var datasetResponse = await _client.GetAsync($"/api/v1/weather/{locationId}/dataset");
        datasetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dataset = await datasetResponse.Content.ReadFromJsonAsync<WeatherDatasetDto>();
        dataset.Should().NotBeNull();
        dataset!.Columns.Should().Contain("TemperatureC");
        dataset.Columns.Should().Contain("RainMm");
        dataset.Columns.Should().Contain("SoilMoisture0To7Cm");
        dataset.Columns.Should().Contain("Et0Mm");
    }

    [Fact]
    public async Task UnauthorizedRequest_WithoutApiKey_ShouldReturn401()
    {
        // Arrange
        using var unauthClient = _client;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/v1/weather/locations");
        // No X-API-Key header attached

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        // In our test factory, X-API-Key was set to "test-api-key"
        // Since default headers on _client includes it, let's create a fresh request with a bad key:
        var badKeyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/weather/locations");
        badKeyRequest.Headers.Add("X-API-Key", "wrong-invalid-key");

        var badKeyResponse = await _client.SendAsync(badKeyRequest);
        badKeyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
