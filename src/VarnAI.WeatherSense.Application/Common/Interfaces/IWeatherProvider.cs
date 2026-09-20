using VarnAI.WeatherSense.Application.Common.Models;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Application.Common.Interfaces;

public interface IWeatherProvider
{
    string ProviderName { get; }

    Task<WeatherProviderResult> GetCurrentAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default);

    Task<WeatherProviderResult> GetForecastAsync(
        WeatherLocation location,
        int forecastDays,
        CancellationToken cancellationToken = default);

    Task<WeatherProviderResult> GetHistoricalAsync(
        WeatherLocation location,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
