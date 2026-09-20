namespace VarnAI.WeatherSense.Application.DTOs.Locations;

public class WeatherLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderLocationId { get; set; }
    public bool IsActive { get; set; }
    public bool CollectionEnabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public class CreateWeatherLocationRequest
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
    public bool CollectionEnabled { get; set; } = true;
}

public class UpdateWeatherLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Timezone { get; set; } = "Asia/Kolkata";
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
    public bool CollectionEnabled { get; set; } = true;
}

public class UpdateLocationStatusRequest
{
    public bool? IsActive { get; set; }
    public bool? CollectionEnabled { get; set; }
}

public class WeatherLocationHeaderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
