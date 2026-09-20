using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Serilog;
using VarnAI.WeatherSense.API.Middleware;
using VarnAI.WeatherSense.Application;
using VarnAI.WeatherSense.Infrastructure;

// 1. Configure Serilog Bootstrap Logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 2. Configure Full Serilog Logger
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/weathersense-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // 3. Add Layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // 4. Add Problem Details & Controllers
    builder.Services.AddProblemDetails();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // 5. Add Health Checks
    var connectionString = builder.Configuration.GetConnectionString("WeatherSenseDb") ?? string.Empty;
    var healthChecks = builder.Services.AddHealthChecks();
    if (!connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(connectionString))
    {
        healthChecks.AddSqlServer(
            connectionString: connectionString,
            name: "sqlserver",
            tags: new[] { "ready", "db" });
    }

    // 6. Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllDev", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // 7. Configure Swagger / OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "VarnAI WeatherSense API",
            Version = "v1",
            Description = "Agricultural weather data platform for VarnAI Farm Fresh. Collects, stores, forecasts, and prepares ML-ready weather datasets.",
            Contact = new OpenApiContact
            {
                Name = "VarnAI Engineering",
                Email = "tech@varnai.com"
            }
        });

        // Configure X-API-Key security in Swagger
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key authentication using the 'X-API-Key' header. Required for production requests.",
            Type = SecuritySchemeType.ApiKey,
            Name = "X-API-Key",
            In = ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });

        var schemeReference = new OpenApiSecuritySchemeReference("ApiKey");

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            [schemeReference] = new List<string>()
        });
    });

    var app = builder.Build();

    // 8. Configure HTTP Middleware Pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VarnAI WeatherSense API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "VarnAI WeatherSense API Documentation";
    });

    app.UseCors("AllowAllDev");

    app.UseMiddleware<ApiKeyAuthMiddleware>();

    app.UseRouting();

    app.MapControllers();

    // 9. Health Check Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Root redirect to swagger in dev
    app.MapGet("/", () => Results.Redirect("/swagger"));

    Log.Information("Starting VarnAI WeatherSense API host...");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
