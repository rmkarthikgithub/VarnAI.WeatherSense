using Microsoft.EntityFrameworkCore;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Application.Common.Interfaces;

public interface IWeatherSenseDbContext
{
    DbSet<WeatherLocation> WeatherLocations { get; }
    DbSet<WeatherObservation> WeatherObservations { get; }
    DbSet<WeatherForecast> WeatherForecasts { get; }
    DbSet<WeatherCollectionExecution> WeatherCollectionExecutions { get; }
    DbSet<WeatherLocationExecutionDetail> WeatherLocationExecutionDetails { get; }
    DbSet<WeatherProviderRawResponse> WeatherProviderRawResponses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
