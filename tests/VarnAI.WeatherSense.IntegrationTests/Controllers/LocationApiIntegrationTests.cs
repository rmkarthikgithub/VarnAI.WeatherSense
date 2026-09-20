using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.IntegrationTests.Fixtures;
using Xunit;

namespace VarnAI.WeatherSense.IntegrationTests.Controllers;

public class LocationApiIntegrationTests : IClassFixture<WeatherSenseTestFactory>
{
    private readonly HttpClient _client;

    public LocationApiIntegrationTests(WeatherSenseTestFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
    }

    [Fact]
    public async Task CreateLocation_AndRetrieveById_ShouldSucceed()
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Tiruppur Dairy Hub",
            Code = "TIRUPPUR_HUB_" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            Latitude = 11.1085m,
            Longitude = 77.3411m,
            Timezone = "Asia/Kolkata",
            Country = "India",
            State = "Tamil Nadu",
            District = "Tiruppur",
            Provider = "OpenMeteo",
            CollectionEnabled = true
        };

        // Act - 1. Create Location
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weather/locations", request);

        // Assert 1
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Code.Should().Be(request.Code);

        // Act - 2. Retrieve Location By ID
        var getResponse = await _client.GetAsync($"/api/v1/weather/locations/{created.Id}");

        // Assert 2
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(request.Name);
        retrieved.Latitude.Should().Be(request.Latitude);
        retrieved.Longitude.Should().Be(request.Longitude);
    }

    [Fact]
    public async Task GetAllLocations_ShouldReturnOkList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/weather/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<WeatherLocationDto>>();
        list.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateLocation_ShouldPersistChanges()
    {
        // Arrange - Create first
        var createRequest = new CreateWeatherLocationRequest
        {
            Name = "Old Location Name",
            Code = "UPDATE_TEST_" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weather/locations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();

        var updateRequest = new UpdateWeatherLocationRequest
        {
            Name = "New Updated Location Name",
            Latitude = 11.5m,
            Longitude = 77.5m,
            Timezone = "Asia/Kolkata",
            CollectionEnabled = false
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/weather/locations/{created!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Updated Location Name");
        updated.CollectionEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLocationStatus_ShouldModifyFlags()
    {
        // Arrange
        var createRequest = new CreateWeatherLocationRequest
        {
            Name = "Status Test Farm",
            Code = "STATUS_TEST_" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/weather/locations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();

        var statusRequest = new UpdateLocationStatusRequest
        {
            IsActive = false,
            CollectionEnabled = false
        };

        // Act
        var patchResponse = await _client.PatchAsJsonAsync($"/api/v1/weather/locations/{created!.Id}/status", statusRequest);

        // Assert
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await patchResponse.Content.ReadFromJsonAsync<WeatherLocationDto>();
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeFalse();
        updated.CollectionEnabled.Should().BeFalse();
    }
}
