using Microsoft.AspNetCore.Mvc;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.DTOs.Weather;
using VarnAI.WeatherSense.Application.Interfaces;

namespace VarnAI.WeatherSense.API.Controllers;

[ApiController]
[Route("api/v1/weather/{locationId:int}")]
[Route("api/v1/weather/locations/{locationId:int}")]
[Produces("application/json")]
public class WeatherDataController : ControllerBase
{
    private readonly IWeatherQueryService _queryService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WeatherDataController(
        IWeatherQueryService queryService,
        IDateTimeProvider dateTimeProvider)
    {
        _queryService = queryService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Gets the current/latest weather observation for the specified location.
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentWeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentWeatherResponse>> GetCurrent(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        var current = await _queryService.GetCurrentAsync(locationId, cancellationToken);
        if (current == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Weather Data Not Found",
                Detail = $"No weather data is available for location ID {locationId}."
            });
        }
        return Ok(current);
    }

    /// <summary>
    /// Gets hourly weather records for the specified location and time range.
    /// </summary>
    [HttpGet("hourly")]
    [ProducesResponseType(typeof(IReadOnlyList<HourlyWeatherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HourlyWeatherDto>>> GetHourly(
        int locationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var fromUtc = from?.ToUniversalTime() ?? now.Date;
        var toUtc = to?.ToUniversalTime() ?? now.Date.AddDays(1).AddTicks(-1);

        if (fromUtc > toUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "'from' date must be earlier than or equal to 'to' date."
            });
        }

        var hourly = await _queryService.GetHourlyAsync(locationId, fromUtc, toUtc, cancellationToken);
        return Ok(hourly);
    }

    /// <summary>
    /// Gets the latest weather forecast snapshot for the specified location.
    /// </summary>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(ForecastResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForecastResponseDto>> GetForecast(
        int locationId,
        [FromQuery] int days = 4,
        CancellationToken cancellationToken = default)
    {
        if (days < 1 || days > 16)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Days Parameter",
                Detail = "'days' must be between 1 and 16."
            });
        }

        var forecast = await _queryService.GetForecastAsync(locationId, days, cancellationToken);
        if (forecast == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Location Not Found",
                Detail = $"Weather location with ID {locationId} was not found."
            });
        }
        return Ok(forecast);
    }

    /// <summary>
    /// Gets historical weather observations with pagination.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(WeatherHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherHistoryResponse>> GetHistory(
        int locationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var fromUtc = from?.ToUniversalTime() ?? now.AddDays(-30);
        var toUtc = to?.ToUniversalTime() ?? now;

        if (fromUtc > toUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "'from' date must be earlier than or equal to 'to' date."
            });
        }

        var history = await _queryService.GetHistoryAsync(locationId, fromUtc, toUtc, pageNumber, pageSize, cancellationToken);
        if (history == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Location Not Found",
                Detail = $"Weather location with ID {locationId} was not found."
            });
        }
        return Ok(history);
    }

    /// <summary>
    /// Gets aggregated weather metrics (temperature extremes, rainfall, wind, ET0, soil moisture) for a date range.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(WeatherSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherSummaryDto>> GetSummary(
        int locationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var fromUtc = from?.ToUniversalTime() ?? now.AddDays(-7);
        var toUtc = to?.ToUniversalTime() ?? now;

        if (fromUtc > toUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "'from' date must be earlier than or equal to 'to' date."
            });
        }

        var summary = await _queryService.GetSummaryAsync(locationId, fromUtc, toUtc, cancellationToken);
        if (summary == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Location Not Found",
                Detail = $"Weather location with ID {locationId} was not found."
            });
        }
        return Ok(summary);
    }

    /// <summary>
    /// Prepares a structured, normalized dataset of weather features suitable for ML/AI models.
    /// </summary>
    [HttpGet("dataset")]
    [ProducesResponseType(typeof(WeatherDatasetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherDatasetDto>> GetDataset(
        int locationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string interval = "hourly",
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var fromUtc = from?.ToUniversalTime() ?? now.AddDays(-30);
        var toUtc = to?.ToUniversalTime() ?? now;

        if (fromUtc > toUtc)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "'from' date must be earlier than or equal to 'to' date."
            });
        }

        var dataset = await _queryService.GetDatasetAsync(locationId, fromUtc, toUtc, interval, cancellationToken);
        if (dataset == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Location Not Found",
                Detail = $"Weather location with ID {locationId} was not found."
            });
        }
        return Ok(dataset);
    }
}
