using VarnAI.WeatherSense.Domain.Common;

namespace VarnAI.WeatherSense.Domain.Entities;

public class WeatherLocation : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string? Country { get; set; } = "India";
    public string? State { get; set; }
    public string? District { get; set; }
    public string Provider { get; set; } = "OpenMeteo";
    public string? ProviderLocationId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CollectionEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ICollection<WeatherObservation> Observations { get; set; } = new List<WeatherObservation>();
    public ICollection<WeatherForecast> Forecasts { get; set; } = new List<WeatherForecast>();
    public ICollection<WeatherLocationExecutionDetail> ExecutionDetails { get; set; } = new List<WeatherLocationExecutionDetail>();
}
