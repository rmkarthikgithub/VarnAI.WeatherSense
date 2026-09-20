using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherProviderRawResponseConfiguration : IEntityTypeConfiguration<WeatherProviderRawResponse>
{
    public void Configure(EntityTypeBuilder<WeatherProviderRawResponse> builder)
    {
        builder.ToTable("WeatherProviderRawResponses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.RequestType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(r => new { r.WeatherLocationId, r.RequestedAtUtc })
            .HasDatabaseName("IX_WeatherProviderRawResponses_Location_RequestedAtUtc");

        builder.HasIndex(r => r.CollectionExecutionId)
            .HasDatabaseName("IX_WeatherProviderRawResponses_CollectionExecutionId");
    }
}
