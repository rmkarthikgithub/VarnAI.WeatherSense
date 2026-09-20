using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VarnAI.WeatherSense.Infrastructure.Options;

namespace VarnAI.WeatherSense.API.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<WeatherSenseSecurityOptions> securityOptions)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Bypass public/infra endpoints
        if (path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/openapi") ||
            path == "/" ||
            path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        var configuredApiKey = securityOptions.Value.ApiKey;

        // In Development, if no API key is set in configuration, allow request with warning
        if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuredApiKey))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) ||
            string.IsNullOrWhiteSpace(extractedApiKey))
        {
            _logger.LogWarning("Unauthorized access attempt to {Path}: Missing '{HeaderName}' header", path, ApiKeyHeaderName);
            await ReturnProblemDetails(context, StatusCodes.Status401Unauthorized, "Missing API Key", $"The '{ApiKeyHeaderName}' header is required.");
            return;
        }

        if (!string.Equals(configuredApiKey, extractedApiKey.ToString(), StringComparison.Ordinal))
        {
            _logger.LogWarning("Unauthorized access attempt to {Path}: Invalid API Key provided", path);
            await ReturnProblemDetails(context, StatusCodes.Status401Unauthorized, "Invalid API Key", "The provided API Key is invalid.");
            return;
        }

        await _next(context);
    }

    private static async Task ReturnProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
