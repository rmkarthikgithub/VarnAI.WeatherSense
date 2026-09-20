using FluentAssertions;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.Application.Validators;
using Xunit;

namespace VarnAI.WeatherSense.UnitTests.Validation;

public class LocationValidationTests
{
    private readonly CreateWeatherLocationRequestValidator _createValidator = new();
    private readonly UpdateWeatherLocationRequestValidator _updateValidator = new();

    [Fact]
    public void ValidLocationRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Emmampoondi Farm",
            Code = "EMMAMPOONDI_FARM",
            Latitude = 11.234567m,
            Longitude = 77.123456m,
            Timezone = "Asia/Kolkata",
            Country = "India",
            State = "Tamil Nadu",
            District = "Tiruppur",
            Provider = "OpenMeteo",
            CollectionEnabled = true
        };

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void InvalidLatitude_ShouldFailValidation(decimal latitude)
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Test Farm",
            Code = "TEST_FARM",
            Latitude = latitude,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata",
            Provider = "OpenMeteo"
        };

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Latitude));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void InvalidLongitude_ShouldFailValidation(decimal longitude)
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Test Farm",
            Code = "TEST_FARM",
            Latitude = 11.0m,
            Longitude = longitude,
            Timezone = "Asia/Kolkata",
            Provider = "OpenMeteo"
        };

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Longitude));
    }

    [Theory]
    [InlineData("Invalid/Timezone_123")]
    [InlineData("UnknownZone")]
    public void InvalidTimezone_ShouldFailValidation(string invalidTimezone)
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Test Farm",
            Code = "TEST_FARM",
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = invalidTimezone,
            Provider = "OpenMeteo"
        };

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Timezone));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("code with spaces")]
    [InlineData("lowercase_code")]
    public void InvalidCode_ShouldFailValidation(string code)
    {
        // Arrange
        var request = new CreateWeatherLocationRequest
        {
            Name = "Test Farm",
            Code = code,
            Latitude = 11.0m,
            Longitude = 77.0m,
            Timezone = "Asia/Kolkata",
            Provider = "OpenMeteo"
        };

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Code));
    }

    [Fact]
    public void UpdateLocation_WithValidData_ShouldPass()
    {
        // Arrange
        var request = new UpdateWeatherLocationRequest
        {
            Name = "Updated Name",
            Latitude = 12.34m,
            Longitude = 78.90m,
            Timezone = "Asia/Kolkata"
        };

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
