using Microsoft.EntityFrameworkCore;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence;

public class WeatherSenseDbContext : DbContext, IWeatherSenseDbContext
{
    public WeatherSenseDbContext(DbContextOptions<WeatherSenseDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherLocation> WeatherLocations => Set<WeatherLocation>();
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
    public DbSet<WeatherCollectionExecution> WeatherCollectionExecutions => Set<WeatherCollectionExecution>();
    public DbSet<WeatherLocationExecutionDetail> WeatherLocationExecutionDetails => Set<WeatherLocationExecutionDetail>();
    public DbSet<WeatherProviderRawResponse> WeatherProviderRawResponses => Set<WeatherProviderRawResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeatherSenseDbContext).Assembly);
    }
}
