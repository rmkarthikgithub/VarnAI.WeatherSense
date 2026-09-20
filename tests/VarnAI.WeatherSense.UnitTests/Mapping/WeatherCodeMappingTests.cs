using FluentAssertions;
using VarnAI.WeatherSense.Application.Mappings;
using VarnAI.WeatherSense.Domain.Enums;
using Xunit;

namespace VarnAI.WeatherSense.UnitTests.Mapping;

public class WeatherCodeMappingTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Clear)]
    [InlineData(1, WeatherCondition.MainlyClear)]
    [InlineData(2, WeatherCondition.PartlyCloudy)]
    [InlineData(3, WeatherCondition.Cloudy)]
    [InlineData(45, WeatherCondition.Fog)]
    [InlineData(48, WeatherCondition.Fog)]
    [InlineData(51, WeatherCondition.Drizzle)]
    [InlineData(53, WeatherCondition.Drizzle)]
    [InlineData(55, WeatherCondition.Drizzle)]
    [InlineData(61, WeatherCondition.Rain)]
    [InlineData(63, WeatherCondition.Rain)]
    [InlineData(65, WeatherCondition.HeavyRain)]
    [InlineData(71, WeatherCondition.Snow)]
    [InlineData(80, WeatherCondition.Rain)]
    [InlineData(82, WeatherCondition.HeavyRain)]
    [InlineData(95, WeatherCondition.Thunderstorm)]
    [InlineData(99, WeatherCondition.Thunderstorm)]
    [InlineData(999, WeatherCondition.Unknown)]
    [InlineData(null, WeatherCondition.Unknown)]
    public void WeatherCode_ShouldMapToExpectedCondition(int? code, WeatherCondition expectedCondition)
    {
        // Act
        var condition = WeatherCodeMapper.ToCondition(code);

        // Assert
        condition.Should().Be(expectedCondition);
    }

    [Fact]
    public void WeatherCode_ShouldMapToFriendlyDescription()
    {
        // Act & Assert
        WeatherCodeMapper.ToDescription(0).Should().Be("Clear sky");
        WeatherCodeMapper.ToDescription(61).Should().Be("Rain: Slight intensity");
        WeatherCodeMapper.ToDescription(95).Should().Be("Thunderstorm: Slight or moderate");
        WeatherCodeMapper.ToDescription(null).Should().Be("Unknown");
    }
}
