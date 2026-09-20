using VarnAI.WeatherSense.Application.DTOs.Collections;

namespace VarnAI.WeatherSense.Application.Interfaces;

public interface IWeatherCollectionService
{
    Task<CollectionSummaryResponse> CollectAllAsync(
        CollectWeatherRequest request,
        string triggerSource,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<LocationCollectionResultDto> CollectLocationAsync(
        int locationId,
        CollectWeatherRequest request,
        string triggerSource,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
