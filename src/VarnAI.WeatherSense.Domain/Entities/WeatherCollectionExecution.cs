using VarnAI.WeatherSense.Domain.Common;
using VarnAI.WeatherSense.Domain.Enums;

namespace VarnAI.WeatherSense.Domain.Entities;

public class WeatherCollectionExecution : BaseEntity<long>
{
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string TriggerSource { get; set; } = CollectionTriggerSource.N8n;
    public int RequestedForecastDays { get; set; } = 4;
    public int LocationsProcessed { get; set; }
    public int LocationsSucceeded { get; set; }
    public int LocationsFailed { get; set; }
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public CollectionExecutionStatus Status { get; set; } = CollectionExecutionStatus.Started;
    public string? ErrorSummary { get; set; }
    public string? CorrelationId { get; set; }

    // Navigation
    public ICollection<WeatherLocationExecutionDetail> Details { get; set; } = new List<WeatherLocationExecutionDetail>();
}
