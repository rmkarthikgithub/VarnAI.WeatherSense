using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VarnAI.WeatherSense.Application.Interfaces;
using VarnAI.WeatherSense.Application.Services;
using VarnAI.WeatherSense.Application.Validators;

namespace VarnAI.WeatherSense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateWeatherLocationRequestValidator>();

        services.AddScoped<IWeatherLocationService, WeatherLocationService>();
        services.AddScoped<IWeatherCollectionService, WeatherCollectionService>();
        services.AddScoped<IWeatherQueryService, WeatherQueryService>();

        return services;
    }
}
