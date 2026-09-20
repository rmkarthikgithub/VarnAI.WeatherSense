using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace VarnAI.WeatherSense.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue("X-Correlation-ID", out var id) ? id?.ToString() : context.TraceIdentifier;

        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation failures have occurred.",
                validationEx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            KeyNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFoundEx.Message,
                null
            ),
            InvalidOperationException invalidOpEx => (
                StatusCodes.Status409Conflict,
                "Conflict / Invalid Operation",
                invalidOpEx.Message,
                null
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Invalid Argument",
                argEx.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment() ? exception.Message : "An unexpected internal server error occurred.",
                null
            )
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "[CorrelationId: {CorrelationId}] Unhandled exception: {Message}", correlationId, exception.Message);
        }
        else
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Client error ({StatusCode}): {Title} - {Detail}",
                correlationId, statusCode, title, detail);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
