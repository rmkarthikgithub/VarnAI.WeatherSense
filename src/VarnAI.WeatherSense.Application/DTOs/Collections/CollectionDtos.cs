using VarnAI.WeatherSense.Domain.Enums;

namespace VarnAI.WeatherSense.Application.DTOs.Collections;

public class CollectWeatherRequest
{
    public int ForecastDays { get; set; } = 4;
}

public class CollectionSummaryResponse
{
    public long ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int RequestedForecastDays { get; set; }
    public int LocationsProcessed { get; set; }
    public int LocationsSucceeded { get; set; }
    public int LocationsFailed { get; set; }
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorSummary { get; set; }
    public List<LocationCollectionResultDto> Locations { get; set; } = new();
}

public class LocationCollectionResultDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Completed", "Failed", "Skipped"
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}

public class CollectionExecutionDto
{
    public long ExecutionId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public int RequestedForecastDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LocationsProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorSummary { get; set; }
    public string? CorrelationId { get; set; }
    public List<LocationExecutionDetailDto> Details { get; set; } = new();
}

public class LocationExecutionDetailDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}
