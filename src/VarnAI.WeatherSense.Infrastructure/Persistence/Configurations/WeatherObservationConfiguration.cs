using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherObservationConfiguration : IEntityTypeConfiguration<WeatherObservation>
{
    public void Configure(EntityTypeBuilder<WeatherObservation> builder)
    {
        builder.ToTable("WeatherObservations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TemperatureC).HasColumnType("decimal(5,2)");
        builder.Property(o => o.FeelsLikeTemperatureC).HasColumnType("decimal(5,2)");
        builder.Property(o => o.RelativeHumidityPercent).HasColumnType("decimal(5,2)");
        builder.Property(o => o.PrecipitationMm).HasColumnType("decimal(6,2)");
        builder.Property(o => o.RainMm).HasColumnType("decimal(6,2)");
        builder.Property(o => o.PrecipitationProbabilityPercent).HasColumnType("decimal(5,2)");
        builder.Property(o => o.WindSpeedKmh).HasColumnType("decimal(5,2)");
        builder.Property(o => o.WindDirectionDegrees).HasColumnType("decimal(5,2)");
        builder.Property(o => o.WindGustKmh).HasColumnType("decimal(5,2)");
        builder.Property(o => o.CloudCoverPercent).HasColumnType("decimal(5,2)");
        builder.Property(o => o.PressureHpa).HasColumnType("decimal(6,2)");
        builder.Property(o => o.SolarRadiationWm2).HasColumnType("decimal(7,2)");
        builder.Property(o => o.Et0Mm).HasColumnType("decimal(5,2)");

        builder.Property(o => o.SoilMoisture0To7Cm).HasColumnType("decimal(6,4)");
        builder.Property(o => o.SoilMoisture7To28Cm).HasColumnType("decimal(6,4)");
        builder.Property(o => o.SoilMoisture28To100Cm).HasColumnType("decimal(6,4)");
        builder.Property(o => o.SoilMoisture100To255Cm).HasColumnType("decimal(6,4)");
        builder.Property(o => o.SoilTemperatureC).HasColumnType("decimal(5,2)");

        builder.Property(o => o.WeatherDescription).HasMaxLength(100);
        builder.Property(o => o.Provider).IsRequired().HasMaxLength(50);
        builder.Property(o => o.ProviderRecordId).HasMaxLength(100);
        builder.Property(o => o.DataSource).IsRequired().HasMaxLength(50);

        // Relationships
        builder.HasOne(o => o.Location)
            .WithMany(l => l.Observations)
            .HasForeignKey(o => o.WeatherLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(o => new { o.WeatherLocationId, o.Provider, o.ObservedAtUtc })
            .IsUnique()
            .HasDatabaseName("IX_WeatherObservations_Location_Provider_ObservedAtUtc");

        builder.HasIndex(o => new { o.WeatherLocationId, o.ObservedAtUtc })
            .HasDatabaseName("IX_WeatherObservations_Location_ObservedAtUtc");
    }
}
