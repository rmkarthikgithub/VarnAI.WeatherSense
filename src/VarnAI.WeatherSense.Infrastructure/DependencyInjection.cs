using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using VarnAI.WeatherSense.Application.Common.Interfaces;
using VarnAI.WeatherSense.Infrastructure.Options;
using VarnAI.WeatherSense.Infrastructure.Persistence;
using VarnAI.WeatherSense.Infrastructure.Providers.OpenMeteo;
using VarnAI.WeatherSense.Infrastructure.Services;

namespace VarnAI.WeatherSense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Connection string configuration
        var connectionString = configuration.GetConnectionString("WeatherSenseDb")
            ?? throw new InvalidOperationException("Connection string 'WeatherSenseDb' was not found.");

        services.AddDbContext<WeatherSenseDbContext>(options =>
        {
            if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
            {
                var dbName = connectionString.Substring("InMemory:".Length);
                options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(dbName) ? "WeatherSense_InMemory" : dbName);
            }
            else
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.MigrationsAssembly(typeof(WeatherSenseDbContext).Assembly.FullName);
                });
            }
        });

        services.AddScoped<IWeatherSenseDbContext>(provider =>
            provider.GetRequiredService<WeatherSenseDbContext>());

        // 2. Options binding
        services.Configure<OpenMeteoOptions>(configuration.GetSection(OpenMeteoOptions.SectionName));
        services.Configure<WeatherCollectionOptions>(configuration.GetSection(WeatherCollectionOptions.SectionName));
        services.Configure<WeatherSenseSecurityOptions>(configuration.GetSection(WeatherSenseSecurityOptions.SectionName));

        // 3. Core infrastructure services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // 4. Resilient Weather Provider HTTP Client
        var openMeteoSection = configuration.GetSection(OpenMeteoOptions.SectionName);
        var baseUrl = openMeteoSection.GetValue<string>("BaseUrl") ?? "https://api.open-meteo.com/";
        var timeoutSeconds = openMeteoSection.GetValue<int?>("TimeoutSeconds") ?? 30;

        services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "VarnAI-WeatherSense/1.0");
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
