using Microsoft.AspNetCore.Mvc;
using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.Interfaces;
using VarnAI.WeatherSense.Domain.Enums;

namespace VarnAI.WeatherSense.API.Controllers;

[ApiController]
[Route("api/v1/weather")]
[Produces("application/json")]
public class WeatherCollectionsController : ControllerBase
{
    private readonly IWeatherCollectionService _collectionService;
    private readonly IWeatherQueryService _queryService;

    public WeatherCollectionsController(
        IWeatherCollectionService collectionService,
        IWeatherQueryService queryService)
    {
        _collectionService = collectionService;
        _queryService = queryService;
    }

    /// <summary>
    /// Triggers weather data collection for all active and collection-enabled locations (Primary n8n integration endpoint).
    /// </summary>
    [HttpPost("collect")]
    [ProducesResponseType(typeof(CollectionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionSummaryResponse>> CollectAll(
        [FromBody] CollectWeatherRequest? request,
        [FromHeader(Name = "X-Trigger-Source")] string? triggerSource,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.Items.TryGetValue("X-Correlation-ID", out var id) ? id?.ToString() : null;
        var actualRequest = request ?? new CollectWeatherRequest { ForecastDays = 4 };
        var source = !string.IsNullOrWhiteSpace(triggerSource) ? triggerSource : CollectionTriggerSource.N8n;

        var result = await _collectionService.CollectAllAsync(actualRequest, source, correlationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Triggers weather data collection for a specific location.
    /// </summary>
    [HttpPost("collect/{locationId:int}")]
    [ProducesResponseType(typeof(LocationCollectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationCollectionResultDto>> CollectLocation(
        int locationId,
        [FromBody] CollectWeatherRequest? request,
        [FromHeader(Name = "X-Trigger-Source")] string? triggerSource,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.Items.TryGetValue("X-Correlation-ID", out var id) ? id?.ToString() : null;
        var actualRequest = request ?? new CollectWeatherRequest { ForecastDays = 4 };
        var source = !string.IsNullOrWhiteSpace(triggerSource) ? triggerSource : CollectionTriggerSource.Manual;

        var result = await _collectionService.CollectLocationAsync(locationId, actualRequest, source, correlationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the latest weather collection execution audit record.
    /// </summary>
    [HttpGet("collections/latest")]
    [ProducesResponseType(typeof(CollectionExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionExecutionDto>> GetLatestCollection(
        CancellationToken cancellationToken = default)
    {
        var execution = await _queryService.GetLatestCollectionExecutionAsync(cancellationToken);
        if (execution == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No Executions Found",
                Detail = "No collection executions have run yet."
            });
        }
        return Ok(execution);
    }

    /// <summary>
    /// Gets details of a specific weather collection execution by execution ID.
    /// </summary>
    [HttpGet("collections/{executionId:long}")]
    [ProducesResponseType(typeof(CollectionExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionExecutionDto>> GetCollectionById(
        long executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _queryService.GetCollectionExecutionByIdAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Execution Not Found",
                Detail = $"Collection execution with ID {executionId} was not found."
            });
        }
        return Ok(execution);
    }
}
