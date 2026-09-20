using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
    public void Configure(EntityTypeBuilder<WeatherForecast> builder)
    {
        builder.ToTable("WeatherForecasts");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.TemperatureC).HasColumnType("decimal(5,2)");
        builder.Property(f => f.FeelsLikeTemperatureC).HasColumnType("decimal(5,2)");
        builder.Property(f => f.RelativeHumidityPercent).HasColumnType("decimal(5,2)");
        builder.Property(f => f.PrecipitationMm).HasColumnType("decimal(6,2)");
        builder.Property(f => f.RainMm).HasColumnType("decimal(6,2)");
        builder.Property(f => f.PrecipitationProbabilityPercent).HasColumnType("decimal(5,2)");
        builder.Property(f => f.WindSpeedKmh).HasColumnType("decimal(5,2)");
        builder.Property(f => f.WindDirectionDegrees).HasColumnType("decimal(5,2)");
        builder.Property(f => f.WindGustKmh).HasColumnType("decimal(5,2)");
        builder.Property(f => f.CloudCoverPercent).HasColumnType("decimal(5,2)");
        builder.Property(f => f.PressureHpa).HasColumnType("decimal(6,2)");
        builder.Property(f => f.SolarRadiationWm2).HasColumnType("decimal(7,2)");
        builder.Property(f => f.Et0Mm).HasColumnType("decimal(5,2)");

        builder.Property(f => f.SoilMoisture0To7Cm).HasColumnType("decimal(6,4)");
        builder.Property(f => f.SoilMoisture7To28Cm).HasColumnType("decimal(6,4)");
        builder.Property(f => f.SoilMoisture28To100Cm).HasColumnType("decimal(6,4)");
        builder.Property(f => f.SoilMoisture100To255Cm).HasColumnType("decimal(6,4)");
        builder.Property(f => f.SoilTemperatureC).HasColumnType("decimal(5,2)");

        builder.Property(f => f.WeatherDescription).HasMaxLength(100);
        builder.Property(f => f.Provider).IsRequired().HasMaxLength(50);
        builder.Property(f => f.ProviderModel).HasMaxLength(50);

        // Relationships
        builder.HasOne(f => f.Location)
            .WithMany(l => l.Forecasts)
            .HasForeignKey(f => f.WeatherLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for preserving forecast snapshots and optimizing queries
        builder.HasIndex(f => new { f.WeatherLocationId, f.Provider, f.ForecastGeneratedAtUtc, f.ForecastForUtc })
            .IsUnique()
            .HasDatabaseName("IX_WeatherForecasts_Snapshot_Unique");

        builder.HasIndex(f => new { f.WeatherLocationId, f.ForecastForUtc })
            .HasDatabaseName("IX_WeatherForecasts_Location_ForecastForUtc");

        builder.HasIndex(f => new { f.WeatherLocationId, f.ForecastGeneratedAtUtc })
            .HasDatabaseName("IX_WeatherForecasts_Location_ForecastGeneratedAtUtc");
    }
}
