using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherCollectionExecutionConfiguration : IEntityTypeConfiguration<WeatherCollectionExecution>
{
    public void Configure(EntityTypeBuilder<WeatherCollectionExecution> builder)
    {
        builder.ToTable("WeatherCollectionExecutions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TriggerSource)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorSummary)
            .HasMaxLength(4000);

        builder.HasIndex(e => e.StartedAtUtc)
            .HasDatabaseName("IX_WeatherCollectionExecutions_StartedAtUtc");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_WeatherCollectionExecutions_Status");
    }
}
