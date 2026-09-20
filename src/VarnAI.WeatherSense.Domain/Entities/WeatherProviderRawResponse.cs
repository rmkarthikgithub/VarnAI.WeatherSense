using VarnAI.WeatherSense.Domain.Common;

namespace VarnAI.WeatherSense.Domain.Entities;

public class WeatherProviderRawResponse : BaseEntity<long>
{
    public int WeatherLocationId { get; set; }
    public long? CollectionExecutionId { get; set; }
    public string Provider { get; set; } = "OpenMeteo";
    public string RequestType { get; set; } = "Forecast"; // "Forecast", "Current", "Historical"
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset ResponseReceivedAtUtc { get; set; }
    public int HttpStatusCode { get; set; }
    public string Payload { get; set; } = string.Empty;
}
