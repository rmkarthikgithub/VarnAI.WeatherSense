using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.DTOs.Weather;

namespace VarnAI.WeatherSense.Application.Interfaces;

public interface IWeatherQueryService
{
    Task<CurrentWeatherResponse?> GetCurrentAsync(
        int locationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HourlyWeatherDto>> GetHourlyAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<ForecastResponseDto?> GetForecastAsync(
        int locationId,
        int days,
        CancellationToken cancellationToken = default);

    Task<WeatherHistoryResponse?> GetHistoryAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<WeatherSummaryDto?> GetSummaryAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<WeatherDatasetDto?> GetDatasetAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string interval = "hourly",
        CancellationToken cancellationToken = default);

    Task<CollectionExecutionDto?> GetLatestCollectionExecutionAsync(
        CancellationToken cancellationToken = default);

    Task<CollectionExecutionDto?> GetCollectionExecutionByIdAsync(
        long executionId,
        CancellationToken cancellationToken = default);
}
