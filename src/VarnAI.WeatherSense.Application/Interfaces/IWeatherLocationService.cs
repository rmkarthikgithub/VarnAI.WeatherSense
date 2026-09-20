using VarnAI.WeatherSense.Application.DTOs.Locations;

namespace VarnAI.WeatherSense.Application.Interfaces;

public interface IWeatherLocationService
{
    Task<IReadOnlyList<WeatherLocationDto>> GetAllAsync(
        bool? isActive = null,
        bool? collectionEnabled = null,
        CancellationToken cancellationToken = default);

    Task<WeatherLocationDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<WeatherLocationDto?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<WeatherLocationDto> CreateAsync(
        CreateWeatherLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<WeatherLocationDto> UpdateAsync(
        int id,
        UpdateWeatherLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<WeatherLocationDto> UpdateStatusAsync(
        int id,
        UpdateLocationStatusRequest request,
        CancellationToken cancellationToken = default);
}
