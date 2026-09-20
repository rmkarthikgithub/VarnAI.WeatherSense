using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.Application.Interfaces;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Application.Services;

public class WeatherLocationService : IWeatherLocationService
{
    private readonly IWeatherSenseDbContext _dbContext;
    private readonly IValidator<CreateWeatherLocationRequest> _createValidator;
    private readonly IValidator<UpdateWeatherLocationRequest> _updateValidator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<WeatherLocationService> _logger;

    public WeatherLocationService(
        IWeatherSenseDbContext dbContext,
        IValidator<CreateWeatherLocationRequest> createValidator,
        IValidator<UpdateWeatherLocationRequest> updateValidator,
        IDateTimeProvider dateTimeProvider,
        ILogger<WeatherLocationService> logger)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WeatherLocationDto>> GetAllAsync(
        bool? isActive = null,
        bool? collectionEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WeatherLocations.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(l => l.IsActive == isActive.Value);
        }

        if (collectionEnabled.HasValue)
        {
            query = query.Where(l => l.CollectionEnabled == collectionEnabled.Value);
        }

        var locations = await query.OrderBy(l => l.Name).ToListAsync(cancellationToken);
        return locations.Select(MapToDto).ToList();
    }

    public async Task<WeatherLocationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return location != null ? MapToDto(location) : null;
    }

    public async Task<WeatherLocationDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == code.ToUpperInvariant(), cancellationToken);

        return location != null ? MapToDto(location) : null;
    }

    public async Task<WeatherLocationDto> CreateAsync(
        CreateWeatherLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var existing = await _dbContext.WeatherLocations
            .AnyAsync(l => l.Code == normalizedCode, cancellationToken);

        if (existing)
        {
            throw new InvalidOperationException($"A weather location with code '{normalizedCode}' already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var location = new WeatherLocation
        {
            Name = request.Name.Trim(),
            Code = normalizedCode,
            Description = request.Description?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Timezone = request.Timezone.Trim(),
            Country = request.Country?.Trim() ?? "India",
            State = request.State?.Trim(),
            District = request.District?.Trim(),
            Provider = string.IsNullOrWhiteSpace(request.Provider) ? "OpenMeteo" : request.Provider.Trim(),
            ProviderLocationId = request.ProviderLocationId?.Trim(),
            IsActive = true,
            CollectionEnabled = request.CollectionEnabled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.WeatherLocations.Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new weather location '{LocationName}' (Code: {LocationCode}, ID: {LocationId})",
            location.Name, location.Code, location.Id);

        return MapToDto(location);
    }

    public async Task<WeatherLocationDto> UpdateAsync(
        int id,
        UpdateWeatherLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var location = await _dbContext.WeatherLocations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            throw new KeyNotFoundException($"Weather location with ID {id} was not found.");
        }

        location.Name = request.Name.Trim();
        location.Description = request.Description?.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.Timezone = request.Timezone.Trim();
        location.Country = request.Country?.Trim();
        location.State = request.State?.Trim();
        location.District = request.District?.Trim();
        location.CollectionEnabled = request.CollectionEnabled;
        location.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated weather location ID {LocationId} ('{LocationName}')", location.Id, location.Name);

        return MapToDto(location);
    }

    public async Task<WeatherLocationDto> UpdateStatusAsync(
        int id,
        UpdateLocationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            throw new KeyNotFoundException($"Weather location with ID {id} was not found.");
        }

        if (request.IsActive.HasValue)
        {
            location.IsActive = request.IsActive.Value;
        }

        if (request.CollectionEnabled.HasValue)
        {
            location.CollectionEnabled = request.CollectionEnabled.Value;
        }

        location.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated status for weather location ID {LocationId}: IsActive={IsActive}, CollectionEnabled={CollectionEnabled}",
            location.Id, location.IsActive, location.CollectionEnabled);

        return MapToDto(location);
    }

    private static WeatherLocationDto MapToDto(WeatherLocation entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        Description = entity.Description,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        Timezone = entity.Timezone,
        Country = entity.Country,
        State = entity.State,
        District = entity.District,
        Provider = entity.Provider,
        ProviderLocationId = entity.ProviderLocationId,
        IsActive = entity.IsActive,
        CollectionEnabled = entity.CollectionEnabled,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };
}
