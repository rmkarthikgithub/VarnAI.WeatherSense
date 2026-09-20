using Microsoft.EntityFrameworkCore;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.DTOs.Locations;
using VarnAI.WeatherSense.Application.DTOs.Weather;
using VarnAI.WeatherSense.Application.Interfaces;
using VarnAI.WeatherSense.Domain.Entities;

namespace VarnAI.WeatherSense.Application.Services;

public class WeatherQueryService : IWeatherQueryService
{
    private readonly IWeatherSenseDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WeatherQueryService(
        IWeatherSenseDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CurrentWeatherResponse?> GetCurrentAsync(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null) return null;

        var header = MapToHeader(location);

        // Try getting latest observation first
        var latestObservation = await _dbContext.WeatherObservations
            .AsNoTracking()
            .Where(o => o.WeatherLocationId == locationId)
            .OrderByDescending(o => o.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestObservation != null)
        {
            return new CurrentWeatherResponse
            {
                Location = header,
                Weather = new CurrentWeatherPointDto
                {
                    ObservedAt = latestObservation.ObservedAtLocal,
                    TemperatureC = latestObservation.TemperatureC,
                    FeelsLikeTemperatureC = latestObservation.FeelsLikeTemperatureC,
                    HumidityPercent = latestObservation.RelativeHumidityPercent,
                    RainMm = latestObservation.RainMm,
                    PrecipitationMm = latestObservation.PrecipitationMm,
                    WindSpeedKmh = latestObservation.WindSpeedKmh,
                    WindDirectionDegrees = latestObservation.WindDirectionDegrees,
                    WindGustKmh = latestObservation.WindGustKmh,
                    PressureHpa = latestObservation.PressureHpa,
                    CloudCoverPercent = latestObservation.CloudCoverPercent,
                    SolarRadiationWm2 = latestObservation.SolarRadiationWm2,
                    Et0Mm = latestObservation.Et0Mm,
                    SoilMoisture0To7Cm = latestObservation.SoilMoisture0To7Cm,
                    SoilMoisture7To28Cm = latestObservation.SoilMoisture7To28Cm,
                    SoilTemperatureC = latestObservation.SoilTemperatureC,
                    WeatherCode = latestObservation.WeatherCode,
                    WeatherDescription = latestObservation.WeatherDescription,
                    DataSource = latestObservation.DataSource
                }
            };
        }

        // Fallback to latest forecast for the current hour
        var nowUtc = _dateTimeProvider.UtcNow;
        var forecastPoint = await _dbContext.WeatherForecasts
            .AsNoTracking()
            .Where(f => f.WeatherLocationId == locationId && f.ForecastForUtc <= nowUtc.AddHours(1))
            .OrderByDescending(f => f.ForecastForUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (forecastPoint != null)
        {
            return new CurrentWeatherResponse
            {
                Location = header,
                Weather = new CurrentWeatherPointDto
                {
                    ObservedAt = forecastPoint.ForecastForLocal,
                    TemperatureC = forecastPoint.TemperatureC,
                    FeelsLikeTemperatureC = forecastPoint.FeelsLikeTemperatureC,
                    HumidityPercent = forecastPoint.RelativeHumidityPercent,
                    RainMm = forecastPoint.RainMm,
                    PrecipitationMm = forecastPoint.PrecipitationMm,
                    WindSpeedKmh = forecastPoint.WindSpeedKmh,
                    WindDirectionDegrees = forecastPoint.WindDirectionDegrees,
                    WindGustKmh = forecastPoint.WindGustKmh,
                    PressureHpa = forecastPoint.PressureHpa,
                    CloudCoverPercent = forecastPoint.CloudCoverPercent,
                    SolarRadiationWm2 = forecastPoint.SolarRadiationWm2,
                    Et0Mm = forecastPoint.Et0Mm,
                    SoilMoisture0To7Cm = forecastPoint.SoilMoisture0To7Cm,
                    SoilMoisture7To28Cm = forecastPoint.SoilMoisture7To28Cm,
                    SoilTemperatureC = forecastPoint.SoilTemperatureC,
                    WeatherCode = forecastPoint.WeatherCode,
                    WeatherDescription = forecastPoint.WeatherDescription,
                    DataSource = $"ForecastModel({forecastPoint.Provider})"
                }
            };
        }

        return null;
    }

    public async Task<IReadOnlyList<HourlyWeatherDto>> GetHourlyAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var observations = await _dbContext.WeatherObservations
            .AsNoTracking()
            .Where(o => o.WeatherLocationId == locationId && o.ObservedAtUtc >= fromUtc && o.ObservedAtUtc <= toUtc)
            .OrderBy(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        return observations.Select(MapToHourlyDto).ToList();
    }

    public async Task<ForecastResponseDto?> GetForecastAsync(
        int locationId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null) return null;

        // Find the latest forecast snapshot generation time
        var latestSnapshotTime = await _dbContext.WeatherForecasts
            .AsNoTracking()
            .Where(f => f.WeatherLocationId == locationId)
            .MaxAsync(f => (DateTimeOffset?)f.ForecastGeneratedAtUtc, cancellationToken);

        if (!latestSnapshotTime.HasValue)
        {
            return new ForecastResponseDto
            {
                Location = MapToHeader(location),
                ForecastGeneratedAtUtc = _dateTimeProvider.UtcNow,
                Days = days
            };
        }

        var maxTimeUtc = latestSnapshotTime.Value.AddDays(days);

        var forecastRecords = await _dbContext.WeatherForecasts
            .AsNoTracking()
            .Where(f => f.WeatherLocationId == locationId
                        && f.ForecastGeneratedAtUtc == latestSnapshotTime.Value
                        && f.ForecastForUtc <= maxTimeUtc)
            .OrderBy(f => f.ForecastForUtc)
            .ToListAsync(cancellationToken);

        var hourly = forecastRecords.Select(f => new HourlyWeatherDto
        {
            TimeUtc = f.ForecastForUtc,
            TimeLocal = f.ForecastForLocal,
            TemperatureC = f.TemperatureC,
            FeelsLikeTemperatureC = f.FeelsLikeTemperatureC,
            RelativeHumidityPercent = f.RelativeHumidityPercent,
            PrecipitationMm = f.PrecipitationMm,
            RainMm = f.RainMm,
            PrecipitationProbabilityPercent = f.PrecipitationProbabilityPercent,
            WindSpeedKmh = f.WindSpeedKmh,
            WindDirectionDegrees = f.WindDirectionDegrees,
            WindGustKmh = f.WindGustKmh,
            CloudCoverPercent = f.CloudCoverPercent,
            PressureHpa = f.PressureHpa,
            SolarRadiationWm2 = f.SolarRadiationWm2,
            Et0Mm = f.Et0Mm,
            SoilMoisture0To7Cm = f.SoilMoisture0To7Cm,
            SoilMoisture7To28Cm = f.SoilMoisture7To28Cm,
            SoilMoisture28To100Cm = f.SoilMoisture28To100Cm,
            SoilMoisture100To255Cm = f.SoilMoisture100To255Cm,
            SoilTemperatureC = f.SoilTemperatureC,
            WeatherCode = f.WeatherCode,
            WeatherDescription = f.WeatherDescription
        }).ToList();

        // Calculate daily summaries
        var dailySummaries = hourly
            .GroupBy(h => DateOnly.FromDateTime(h.TimeLocal.Date))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var temps = g.Where(x => x.TemperatureC.HasValue).Select(x => x.TemperatureC!.Value).ToList();
                var humidities = g.Where(x => x.RelativeHumidityPercent.HasValue).Select(x => x.RelativeHumidityPercent!.Value).ToList();
                var rainProbabilities = g.Where(x => x.PrecipitationProbabilityPercent.HasValue).Select(x => x.PrecipitationProbabilityPercent!.Value).ToList();
                var winds = g.Where(x => x.WindSpeedKmh.HasValue).Select(x => x.WindSpeedKmh!.Value).ToList();
                var soils = g.Where(x => x.SoilMoisture0To7Cm.HasValue).Select(x => x.SoilMoisture0To7Cm!.Value).ToList();

                var dominantCodeGroup = g.Where(x => x.WeatherCode.HasValue)
                    .GroupBy(x => x.WeatherCode!.Value)
                    .OrderByDescending(gr => gr.Count())
                    .FirstOrDefault();

                return new DailyForecastSummaryDto
                {
                    DateLocal = g.Key,
                    MinTemperatureC = temps.Count > 0 ? temps.Min() : null,
                    MaxTemperatureC = temps.Count > 0 ? temps.Max() : null,
                    AvgHumidityPercent = humidities.Count > 0 ? Math.Round(humidities.Average(), 1) : null,
                    TotalRainMm = Math.Round(g.Sum(x => x.RainMm ?? 0), 2),
                    MaxRainProbabilityPercent = rainProbabilities.Count > 0 ? rainProbabilities.Max() : null,
                    MaxWindSpeedKmh = winds.Count > 0 ? winds.Max() : null,
                    TotalEt0Mm = Math.Round(g.Sum(x => x.Et0Mm ?? 0), 2),
                    AvgSoilMoisture0To7Cm = soils.Count > 0 ? Math.Round(soils.Average(), 4) : null,
                    DominantWeatherCode = dominantCodeGroup?.Key,
                    DominantWeatherDescription = dominantCodeGroup?.First().WeatherDescription
                };
            }).ToList();

        return new ForecastResponseDto
        {
            Location = MapToHeader(location),
            ForecastGeneratedAtUtc = latestSnapshotTime.Value,
            Days = days,
            Hourly = hourly,
            DailySummary = dailySummaries
        };
    }

    public async Task<WeatherHistoryResponse?> GetHistoryAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null) return null;

        var query = _dbContext.WeatherObservations
            .AsNoTracking()
            .Where(o => o.WeatherLocationId == locationId && o.ObservedAtUtc >= fromUtc && o.ObservedAtUtc <= toUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var validPageNumber = Math.Max(1, pageNumber);
        var validPageSize = Math.Clamp(pageSize, 1, 1000);

        var records = await query
            .OrderBy(o => o.ObservedAtUtc)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        return new WeatherHistoryResponse
        {
            Location = MapToHeader(location),
            TotalCount = totalCount,
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            Records = records.Select(MapToHourlyDto).ToList()
        };
    }

    public async Task<WeatherSummaryDto?> GetSummaryAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null) return null;

        var observations = await _dbContext.WeatherObservations
            .AsNoTracking()
            .Where(o => o.WeatherLocationId == locationId && o.ObservedAtUtc >= fromUtc && o.ObservedAtUtc <= toUtc)
            .ToListAsync(cancellationToken);

        if (observations.Count == 0)
        {
            return new WeatherSummaryDto
            {
                Location = MapToHeader(location),
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TotalHoursCount = 0,
                RainyHoursCount = 0
            };
        }

        var temps = observations.Where(x => x.TemperatureC.HasValue).Select(x => x.TemperatureC!.Value).ToList();
        var humidities = observations.Where(x => x.RelativeHumidityPercent.HasValue).Select(x => x.RelativeHumidityPercent!.Value).ToList();
        var winds = observations.Where(x => x.WindSpeedKmh.HasValue).Select(x => x.WindSpeedKmh!.Value).ToList();
        var soils = observations.Where(x => x.SoilMoisture0To7Cm.HasValue).Select(x => x.SoilMoisture0To7Cm!.Value).ToList();
        var rains = observations.Where(x => x.RainMm.HasValue).Select(x => x.RainMm!.Value).ToList();

        var rainyHours = observations.Count(x => (x.RainMm.HasValue && x.RainMm.Value > 0) || (x.PrecipitationMm.HasValue && x.PrecipitationMm.Value > 0));

        return new WeatherSummaryDto
        {
            Location = MapToHeader(location),
            FromUtc = fromUtc,
            ToUtc = toUtc,
            MinTemperatureC = temps.Count > 0 ? temps.Min() : null,
            MaxTemperatureC = temps.Count > 0 ? temps.Max() : null,
            AvgTemperatureC = temps.Count > 0 ? Math.Round(temps.Average(), 2) : null,
            AvgHumidityPercent = humidities.Count > 0 ? Math.Round(humidities.Average(), 2) : null,
            TotalRainfallMm = Math.Round(observations.Sum(x => x.RainMm ?? 0), 2),
            MaxHourlyRainfallMm = rains.Count > 0 ? rains.Max() : null,
            AvgWindSpeedKmh = winds.Count > 0 ? Math.Round(winds.Average(), 2) : null,
            MaxWindSpeedKmh = winds.Count > 0 ? winds.Max() : null,
            AvgSoilMoisture0To7Cm = soils.Count > 0 ? Math.Round(soils.Average(), 4) : null,
            MinSoilMoisture0To7Cm = soils.Count > 0 ? soils.Min() : null,
            MaxSoilMoisture0To7Cm = soils.Count > 0 ? soils.Max() : null,
            TotalEt0Mm = Math.Round(observations.Sum(x => x.Et0Mm ?? 0), 2),
            RainyHoursCount = rainyHours,
            TotalHoursCount = observations.Count
        };
    }

    public async Task<WeatherDatasetDto?> GetDatasetAsync(
        int locationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string interval = "hourly",
        CancellationToken cancellationToken = default)
    {
        var location = await _dbContext.WeatherLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null) return null;

        var observations = await _dbContext.WeatherObservations
            .AsNoTracking()
            .Where(o => o.WeatherLocationId == locationId && o.ObservedAtUtc >= fromUtc && o.ObservedAtUtc <= toUtc)
            .OrderBy(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        var columns = new List<string>
        {
            "TimestampUtc",
            "TimestampLocal",
            "TemperatureC",
            "FeelsLikeTemperatureC",
            "RelativeHumidityPercent",
            "PrecipitationMm",
            "RainMm",
            "PrecipitationProbabilityPercent",
            "WindSpeedKmh",
            "WindDirectionDegrees",
            "WindGustKmh",
            "CloudCoverPercent",
            "PressureHpa",
            "SolarRadiationWm2",
            "Et0Mm",
            "SoilMoisture0To7Cm",
            "SoilMoisture7To28Cm",
            "SoilMoisture28To100Cm",
            "SoilMoisture100To255Cm",
            "SoilTemperatureC",
            "WeatherCode",
            "WeatherDescription"
        };

        var rows = observations.Select(o => new WeatherDatasetRowDto
        {
            TimestampUtc = o.ObservedAtUtc,
            TimestampLocal = o.ObservedAtLocal,
            TemperatureC = o.TemperatureC,
            FeelsLikeTemperatureC = o.FeelsLikeTemperatureC,
            RelativeHumidityPercent = o.RelativeHumidityPercent,
            PrecipitationMm = o.PrecipitationMm,
            RainMm = o.RainMm,
            PrecipitationProbabilityPercent = o.PrecipitationProbabilityPercent,
            WindSpeedKmh = o.WindSpeedKmh,
            WindDirectionDegrees = o.WindDirectionDegrees,
            WindGustKmh = o.WindGustKmh,
            CloudCoverPercent = o.CloudCoverPercent,
            PressureHpa = o.PressureHpa,
            SolarRadiationWm2 = o.SolarRadiationWm2,
            Et0Mm = o.Et0Mm,
            SoilMoisture0To7Cm = o.SoilMoisture0To7Cm,
            SoilMoisture7To28Cm = o.SoilMoisture7To28Cm,
            SoilMoisture28To100Cm = o.SoilMoisture28To100Cm,
            SoilMoisture100To255Cm = o.SoilMoisture100To255Cm,
            SoilTemperatureC = o.SoilTemperatureC,
            WeatherCode = o.WeatherCode,
            WeatherDescription = o.WeatherDescription
        }).ToList();

        return new WeatherDatasetDto
        {
            Location = MapToHeader(location),
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Interval = interval,
            RowCount = rows.Count,
            Columns = columns,
            Rows = rows
        };
    }

    public async Task<CollectionExecutionDto?> GetLatestCollectionExecutionAsync(
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WeatherCollectionExecutions
            .AsNoTracking()
            .Include(e => e.Details)
            .ThenInclude(d => d.Location)
            .OrderByDescending(e => e.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return execution != null ? MapToExecutionDto(execution) : null;
    }

    public async Task<CollectionExecutionDto?> GetCollectionExecutionByIdAsync(
        long executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WeatherCollectionExecutions
            .AsNoTracking()
            .Include(e => e.Details)
            .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        return execution != null ? MapToExecutionDto(execution) : null;
    }

    private static WeatherLocationHeaderDto MapToHeader(WeatherLocation location) => new()
    {
        Id = location.Id,
        Name = location.Name,
        Code = location.Code,
        Timezone = location.Timezone,
        Latitude = location.Latitude,
        Longitude = location.Longitude
    };

    private static HourlyWeatherDto MapToHourlyDto(WeatherObservation o) => new()
    {
        TimeUtc = o.ObservedAtUtc,
        TimeLocal = o.ObservedAtLocal,
        TemperatureC = o.TemperatureC,
        FeelsLikeTemperatureC = o.FeelsLikeTemperatureC,
        RelativeHumidityPercent = o.RelativeHumidityPercent,
        PrecipitationMm = o.PrecipitationMm,
        RainMm = o.RainMm,
        PrecipitationProbabilityPercent = o.PrecipitationProbabilityPercent,
        WindSpeedKmh = o.WindSpeedKmh,
        WindDirectionDegrees = o.WindDirectionDegrees,
        WindGustKmh = o.WindGustKmh,
        CloudCoverPercent = o.CloudCoverPercent,
        PressureHpa = o.PressureHpa,
        SolarRadiationWm2 = o.SolarRadiationWm2,
        Et0Mm = o.Et0Mm,
        SoilMoisture0To7Cm = o.SoilMoisture0To7Cm,
        SoilMoisture7To28Cm = o.SoilMoisture7To28Cm,
        SoilMoisture28To100Cm = o.SoilMoisture28To100Cm,
        SoilMoisture100To255Cm = o.SoilMoisture100To255Cm,
        SoilTemperatureC = o.SoilTemperatureC,
        WeatherCode = o.WeatherCode,
        WeatherDescription = o.WeatherDescription
    };

    private static CollectionExecutionDto MapToExecutionDto(WeatherCollectionExecution entity) => new()
    {
        ExecutionId = entity.Id,
        StartedAtUtc = entity.StartedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        TriggerSource = entity.TriggerSource,
        RequestedForecastDays = entity.RequestedForecastDays,
        Status = entity.Status.ToString(),
        LocationsProcessed = entity.LocationsProcessed,
        SuccessCount = entity.LocationsSucceeded,
        FailureCount = entity.LocationsFailed,
        RecordsInserted = entity.RecordsInserted,
        RecordsSkipped = entity.RecordsSkipped,
        ErrorSummary = entity.ErrorSummary,
        CorrelationId = entity.CorrelationId,
        Details = entity.Details.Select(d => new LocationExecutionDetailDto
        {
            LocationId = d.WeatherLocationId,
            LocationName = d.Location?.Name ?? string.Empty,
            LocationCode = d.Location?.Code ?? string.Empty,
            Status = d.Status,
            RecordsInserted = d.RecordsInserted,
            RecordsSkipped = d.RecordsSkipped,
            ErrorMessage = d.ErrorMessage,
            DurationMs = d.DurationMs
        }).ToList()
    };
}
