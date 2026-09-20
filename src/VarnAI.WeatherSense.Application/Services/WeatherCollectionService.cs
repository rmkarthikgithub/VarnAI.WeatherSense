using System.Diagnostics;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Application.DTOs.Collections;
using VarnAI.WeatherSense.Application.Interfaces;
using VarnAI.WeatherSense.Domain.Entities;
using VarnAI.WeatherSense.Domain.Enums;

namespace VarnAI.WeatherSense.Application.Services;

public class WeatherCollectionService : IWeatherCollectionService
{
    private readonly IWeatherSenseDbContext _dbContext;
    private readonly IWeatherProvider _weatherProvider;
    private readonly IValidator<CollectWeatherRequest> _collectValidator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<WeatherCollectionService> _logger;

    public WeatherCollectionService(
        IWeatherSenseDbContext dbContext,
        IWeatherProvider weatherProvider,
        IValidator<CollectWeatherRequest> collectValidator,
        IDateTimeProvider dateTimeProvider,
        ILogger<WeatherCollectionService> logger)
    {
        _dbContext = dbContext;
        _weatherProvider = weatherProvider;
        _collectValidator = collectValidator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<CollectionSummaryResponse> CollectAllAsync(
        CollectWeatherRequest request,
        string triggerSource,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        await _collectValidator.ValidateAndThrowAsync(request, cancellationToken);

        var startedAt = _dateTimeProvider.UtcNow;
        var execution = new WeatherCollectionExecution
        {
            StartedAtUtc = startedAt,
            TriggerSource = string.IsNullOrWhiteSpace(triggerSource) ? CollectionTriggerSource.N8n : triggerSource,
            RequestedForecastDays = request.ForecastDays,
            Status = CollectionExecutionStatus.Started,
            CorrelationId = correlationId,
            CreatedAtUtc = startedAt
        };

        _dbContext.WeatherCollectionExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Weather collection execution {ExecutionId} started. TriggerSource: {TriggerSource}, CorrelationId: {CorrelationId}",
            execution.Id, execution.TriggerSource, execution.CorrelationId);

        var locations = await _dbContext.WeatherLocations
            .Where(l => l.IsActive && l.CollectionEnabled)
            .ToListAsync(cancellationToken);

        var summary = new CollectionSummaryResponse
        {
            ExecutionId = execution.Id,
            StartedAtUtc = startedAt,
            RequestedForecastDays = request.ForecastDays,
            LocationsProcessed = locations.Count
        };

        var errors = new List<string>();

        foreach (var location in locations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var locationResult = await ProcessSingleLocationAsync(execution.Id, location, request.ForecastDays, cancellationToken);
            summary.Locations.Add(locationResult);

            if (locationResult.Status == "Completed")
            {
                summary.LocationsSucceeded++;
                summary.RecordsInserted += locationResult.RecordsInserted;
                summary.RecordsSkipped += locationResult.RecordsSkipped;
            }
            else
            {
                summary.LocationsFailed++;
                if (!string.IsNullOrWhiteSpace(locationResult.Error))
                {
                    errors.Add($"[{location.Code}] {locationResult.Error}");
                }
            }
        }

        var completedAt = _dateTimeProvider.UtcNow;
        var finalStatus = summary.LocationsFailed == 0
            ? CollectionExecutionStatus.Completed
            : (summary.LocationsSucceeded > 0 ? CollectionExecutionStatus.CompletedWithErrors : CollectionExecutionStatus.Failed);

        execution.CompletedAtUtc = completedAt;
        execution.LocationsProcessed = summary.LocationsProcessed;
        execution.LocationsSucceeded = summary.LocationsSucceeded;
        execution.LocationsFailed = summary.LocationsFailed;
        execution.RecordsInserted = summary.RecordsInserted;
        execution.RecordsSkipped = summary.RecordsSkipped;
        execution.Status = finalStatus;
        execution.ErrorSummary = errors.Count > 0 ? string.Join("; ", errors) : null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        summary.CompletedAtUtc = completedAt;
        summary.Status = finalStatus.ToString();
        summary.ErrorSummary = execution.ErrorSummary;

        _logger.LogInformation("Weather collection execution {ExecutionId} finished with status {Status}. Processed: {Processed}, Succeeded: {Succeeded}, Failed: {Failed}, Inserted: {Inserted}, Skipped: {Skipped}",
            execution.Id, finalStatus, summary.LocationsProcessed, summary.LocationsSucceeded, summary.LocationsFailed, summary.RecordsInserted, summary.RecordsSkipped);

        return summary;
    }

    public async Task<LocationCollectionResultDto> CollectLocationAsync(
        int locationId,
        CollectWeatherRequest request,
        string triggerSource,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        await _collectValidator.ValidateAndThrowAsync(request, cancellationToken);

        var location = await _dbContext.WeatherLocations
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location == null)
        {
            throw new KeyNotFoundException($"Weather location with ID {locationId} was not found.");
        }

        var startedAt = _dateTimeProvider.UtcNow;
        var execution = new WeatherCollectionExecution
        {
            StartedAtUtc = startedAt,
            TriggerSource = string.IsNullOrWhiteSpace(triggerSource) ? CollectionTriggerSource.Manual : triggerSource,
            RequestedForecastDays = request.ForecastDays,
            LocationsProcessed = 1,
            Status = CollectionExecutionStatus.Started,
            CorrelationId = correlationId,
            CreatedAtUtc = startedAt
        };

        _dbContext.WeatherCollectionExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = await ProcessSingleLocationAsync(execution.Id, location, request.ForecastDays, cancellationToken);

        execution.CompletedAtUtc = _dateTimeProvider.UtcNow;
        execution.LocationsSucceeded = result.Status == "Completed" ? 1 : 0;
        execution.LocationsFailed = result.Status == "Completed" ? 0 : 1;
        execution.RecordsInserted = result.RecordsInserted;
        execution.RecordsSkipped = result.RecordsSkipped;
        execution.Status = result.Status == "Completed" ? CollectionExecutionStatus.Completed : CollectionExecutionStatus.Failed;
        execution.ErrorSummary = result.Error;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<LocationCollectionResultDto> ProcessSingleLocationAsync(
        long executionId,
        WeatherLocation location,
        int forecastDays,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new LocationCollectionResultDto
        {
            LocationId = location.Id,
            LocationName = location.Name,
            LocationCode = location.Code
        };

        try
        {
            _logger.LogInformation("Collecting weather data for location {LocationCode} (ID: {LocationId})", location.Code, location.Id);

            var providerResult = await _weatherProvider.GetForecastAsync(location, forecastDays, cancellationToken);

            if (!providerResult.Success)
            {
                result.Status = "Failed";
                result.Error = providerResult.Error ?? "Weather provider returned an unspecified error.";
                _logger.LogWarning("Provider collection failed for {LocationCode}: {Error}", location.Code, result.Error);

                await RecordExecutionDetailAsync(executionId, location.Id, "Failed", 0, 0, result.Error, stopwatch.ElapsedMilliseconds, cancellationToken);
                return result;
            }

            // Store raw response if available
            if (!string.IsNullOrWhiteSpace(providerResult.RawJson))
            {
                var rawResponse = new WeatherProviderRawResponse
                {
                    WeatherLocationId = location.Id,
                    CollectionExecutionId = executionId,
                    Provider = _weatherProvider.ProviderName,
                    RequestType = "Forecast",
                    RequestedAtUtc = _dateTimeProvider.UtcNow,
                    ResponseReceivedAtUtc = _dateTimeProvider.UtcNow,
                    HttpStatusCode = providerResult.HttpStatusCode ?? 200,
                    Payload = providerResult.RawJson,
                    CreatedAtUtc = _dateTimeProvider.UtcNow
                };
                _dbContext.WeatherProviderRawResponses.Add(rawResponse);
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var generatedAtUtc = providerResult.GeneratedAtUtc != default ? providerResult.GeneratedAtUtc : nowUtc;
            int insertedCount = 0;
            int skippedCount = 0;

            // 1. Process Observations (Historical & Current data points up to generatedAtUtc)
            var observationCandidates = providerResult.Hourly
                .Where(p => p.TimeUtc <= nowUtc.AddMinutes(30))
                .ToList();

            if (observationCandidates.Count > 0)
            {
                var minObsTime = observationCandidates.Min(p => p.TimeUtc);
                var maxObsTime = observationCandidates.Max(p => p.TimeUtc);

                var existingObsTimes = await _dbContext.WeatherObservations
                    .Where(o => o.WeatherLocationId == location.Id
                                && o.Provider == _weatherProvider.ProviderName
                                && o.ObservedAtUtc >= minObsTime
                                && o.ObservedAtUtc <= maxObsTime)
                    .Select(o => o.ObservedAtUtc)
                    .ToListAsync(cancellationToken);

                var existingObsSet = new HashSet<DateTimeOffset>(existingObsTimes);

                foreach (var point in observationCandidates)
                {
                    if (existingObsSet.Contains(point.TimeUtc))
                    {
                        skippedCount++;
                        continue;
                    }

                    var observation = new WeatherObservation
                    {
                        WeatherLocationId = location.Id,
                        ObservedAtUtc = point.TimeUtc,
                        ObservedAtLocal = point.TimeLocal,
                        TemperatureC = point.TemperatureC,
                        FeelsLikeTemperatureC = point.FeelsLikeTemperatureC,
                        RelativeHumidityPercent = point.RelativeHumidityPercent,
                        PrecipitationMm = point.PrecipitationMm,
                        RainMm = point.RainMm,
                        PrecipitationProbabilityPercent = point.PrecipitationProbabilityPercent,
                        WindSpeedKmh = point.WindSpeedKmh,
                        WindDirectionDegrees = point.WindDirectionDegrees,
                        WindGustKmh = point.WindGustKmh,
                        CloudCoverPercent = point.CloudCoverPercent,
                        PressureHpa = point.PressureHpa,
                        SolarRadiationWm2 = point.SolarRadiationWm2,
                        Et0Mm = point.Et0Mm,
                        SoilMoisture0To7Cm = point.SoilMoisture0To7Cm,
                        SoilMoisture7To28Cm = point.SoilMoisture7To28Cm,
                        SoilMoisture28To100Cm = point.SoilMoisture28To100Cm,
                        SoilMoisture100To255Cm = point.SoilMoisture100To255Cm,
                        SoilTemperatureC = point.SoilTemperatureC,
                        WeatherCode = point.WeatherCode,
                        WeatherDescription = point.WeatherDescription,
                        Provider = _weatherProvider.ProviderName,
                        DataSource = WeatherDataSource.OpenMeteo,
                        CreatedAtUtc = nowUtc
                    };

                    _dbContext.WeatherObservations.Add(observation);
                    existingObsSet.Add(point.TimeUtc);
                    insertedCount++;
                }
            }

            // 2. Process Forecast Snapshots (Preserve all snapshot records for accuracy analysis)
            // Look up existing forecast records for this exact snapshot (location + provider + generatedAtUtc)
            var existingForecastTimes = await _dbContext.WeatherForecasts
                .Where(f => f.WeatherLocationId == location.Id
                            && f.Provider == _weatherProvider.ProviderName
                            && f.ForecastGeneratedAtUtc == generatedAtUtc)
                .Select(f => f.ForecastForUtc)
                .ToListAsync(cancellationToken);

            var existingForecastSet = new HashSet<DateTimeOffset>(existingForecastTimes);

            foreach (var point in providerResult.Hourly)
            {
                if (existingForecastSet.Contains(point.TimeUtc))
                {
                    skippedCount++;
                    continue;
                }

                var forecast = new WeatherForecast
                {
                    WeatherLocationId = location.Id,
                    ForecastGeneratedAtUtc = generatedAtUtc,
                    ForecastForUtc = point.TimeUtc,
                    ForecastForLocal = point.TimeLocal,
                    TemperatureC = point.TemperatureC,
                    FeelsLikeTemperatureC = point.FeelsLikeTemperatureC,
                    RelativeHumidityPercent = point.RelativeHumidityPercent,
                    PrecipitationMm = point.PrecipitationMm,
                    RainMm = point.RainMm,
                    PrecipitationProbabilityPercent = point.PrecipitationProbabilityPercent,
                    WindSpeedKmh = point.WindSpeedKmh,
                    WindDirectionDegrees = point.WindDirectionDegrees,
                    WindGustKmh = point.WindGustKmh,
                    CloudCoverPercent = point.CloudCoverPercent,
                    PressureHpa = point.PressureHpa,
                    SolarRadiationWm2 = point.SolarRadiationWm2,
                    Et0Mm = point.Et0Mm,
                    SoilMoisture0To7Cm = point.SoilMoisture0To7Cm,
                    SoilMoisture7To28Cm = point.SoilMoisture7To28Cm,
                    SoilMoisture28To100Cm = point.SoilMoisture28To100Cm,
                    SoilMoisture100To255Cm = point.SoilMoisture100To255Cm,
                    SoilTemperatureC = point.SoilTemperatureC,
                    WeatherCode = point.WeatherCode,
                    WeatherDescription = point.WeatherDescription,
                    Provider = _weatherProvider.ProviderName,
                    ProviderModel = providerResult.Model,
                    CreatedAtUtc = nowUtc
                };

                _dbContext.WeatherForecasts.Add(forecast);
                existingForecastSet.Add(point.TimeUtc);
                insertedCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            result.Status = "Completed";
            result.RecordsInserted = insertedCount;
            result.RecordsSkipped = skippedCount;
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            await RecordExecutionDetailAsync(executionId, location.Id, "Completed", insertedCount, skippedCount, null, stopwatch.ElapsedMilliseconds, cancellationToken);

            _logger.LogInformation("Successfully collected weather for {LocationCode}. Inserted: {Inserted}, Skipped: {Skipped}, Duration: {Duration}ms",
                location.Code, insertedCount, skippedCount, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Status = "Failed";
            result.Error = ex.Message;
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Unexpected exception collecting weather for {LocationCode}: {Message}", location.Code, ex.Message);

            await RecordExecutionDetailAsync(executionId, location.Id, "Failed", 0, 0, ex.Message, stopwatch.ElapsedMilliseconds, cancellationToken);
            return result;
        }
    }

    private async Task RecordExecutionDetailAsync(
        long executionId,
        int locationId,
        string status,
        int inserted,
        int skipped,
        string? error,
        long durationMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = new WeatherLocationExecutionDetail
            {
                CollectionExecutionId = executionId,
                WeatherLocationId = locationId,
                Status = status,
                RecordsInserted = inserted,
                RecordsSkipped = skipped,
                ErrorMessage = error,
                DurationMs = durationMs,
                CreatedAtUtc = _dateTimeProvider.UtcNow
            };

            _dbContext.WeatherLocationExecutionDetails.Add(detail);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record execution detail for execution {ExecutionId}, location {LocationId}", executionId, locationId);
        }
    }
}
