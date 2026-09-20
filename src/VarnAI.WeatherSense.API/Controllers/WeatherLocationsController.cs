using Microsoft.AspNetCore.Mvc;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.Application.Interfaces;

namespace VarnAI.WeatherSense.API.Controllers;

[ApiController]
[Route("api/v1/weather/locations")]
[Route("api/v1/locations")]
[Produces("application/json")]
public class WeatherLocationsController : ControllerBase
{
    private readonly IWeatherLocationService _locationService;

    public WeatherLocationsController(IWeatherLocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>
    /// Gets all configurable weather locations with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WeatherLocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WeatherLocationDto>>> GetAll(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? collectionEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationService.GetAllAsync(isActive, collectionEnabled, cancellationToken);
        return Ok(locations);
    }

    /// <summary>
    /// Gets a weather location by ID.
    /// </summary>
    [HttpGet("{locationId:int}")]
    [ProducesResponseType(typeof(WeatherLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherLocationDto>> GetById(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, cancellationToken);
        if (location == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Location Not Found",
                Detail = $"Weather location with ID {locationId} was not found."
            });
        }
        return Ok(location);
    }

    /// <summary>
    /// Creates a new weather location with geographical coordinates and timezone.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WeatherLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WeatherLocationDto>> Create(
        [FromBody] CreateWeatherLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await _locationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { locationId = location.Id }, location);
    }

    /// <summary>
    /// Updates details of an existing weather location.
    /// </summary>
    [HttpPut("{locationId:int}")]
    [ProducesResponseType(typeof(WeatherLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherLocationDto>> Update(
        int locationId,
        [FromBody] UpdateWeatherLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _locationService.UpdateAsync(locationId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Updates active or collection status of a weather location.
    /// </summary>
    [HttpPatch("{locationId:int}/status")]
    [ProducesResponseType(typeof(WeatherLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherLocationDto>> UpdateStatus(
        int locationId,
        [FromBody] UpdateLocationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _locationService.UpdateStatusAsync(locationId, request, cancellationToken);
        return Ok(updated);
    }
}
