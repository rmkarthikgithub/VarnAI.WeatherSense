using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Configurations;

public class WeatherLocationExecutionDetailConfiguration : IEntityTypeConfiguration<WeatherLocationExecutionDetail>
{
    public void Configure(EntityTypeBuilder<WeatherLocationExecutionDetail> builder)
    {
        builder.ToTable("WeatherLocationExecutionDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(4000);

        builder.HasOne(d => d.Execution)
            .WithMany(e => e.Details)
            .HasForeignKey(d => d.CollectionExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Location)
            .WithMany(l => l.ExecutionDetails)
            .HasForeignKey(d => d.WeatherLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.CollectionExecutionId, d.WeatherLocationId })
            .HasDatabaseName("IX_WeatherLocationExecutionDetails_Execution_Location");
    }
}
