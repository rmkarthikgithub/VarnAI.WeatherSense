using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherLocationConfiguration : IEntityTypeConfiguration<WeatherLocation>
{
    public void Configure(EntityTypeBuilder<WeatherLocation> builder)
    {
        builder.ToTable("WeatherLocations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.Latitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(l => l.Longitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(l => l.Timezone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Country)
            .HasMaxLength(100);

        builder.Property(l => l.State)
            .HasMaxLength(100);

        builder.Property(l => l.District)
            .HasMaxLength(100);

        builder.Property(l => l.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.ProviderLocationId)
            .HasMaxLength(100);

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.CollectionEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("IX_WeatherLocations_Code");

        builder.HasIndex(l => new { l.IsActive, l.CollectionEnabled })
            .HasDatabaseName("IX_WeatherLocations_IsActive_CollectionEnabled");
    }
}
