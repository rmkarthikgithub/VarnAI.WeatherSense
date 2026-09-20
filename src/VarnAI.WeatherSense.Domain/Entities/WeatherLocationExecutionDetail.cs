using VarnAI.WeatherSense.Domain.Common;

namespace VarnAI.WeatherSense.Domain.Entities;

public class WeatherLocationExecutionDetail : BaseEntity<long>
{
    public long CollectionExecutionId { get; set; }
    public int WeatherLocationId { get; set; }
    public string Status { get; set; } = "Completed"; // "Completed", "Failed", "Skipped"
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }

    // Navigation
    public WeatherCollectionExecution Execution { get; set; } = null!;
    public WeatherLocation Location { get; set; } = null!;
}
