using System.Net;
using FluentAssertions;
using VarnAI.WeatherSense.IntegrationTests.Fixtures;
using Xunit;

namespace VarnAI.WeatherSense.IntegrationTests.Controllers;

public class HealthCheckIntegrationTests : IClassFixture<WeatherSenseTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(WeatherSenseTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLiveEndpoint_ShouldReturnHealthyWithoutAuth()
    {
        // Act - No API key header needed for /health/live
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
