using System.Text.Json;
using FluentValidation;
using HotelPro.Domain.Exceptions;

namespace HotelPro.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        var (statusCode, title, errors) = exception switch
        {
            DomainException domainEx => (domainEx.StatusCode, domainEx.Message, (IDictionary<string, string[]>?)null),
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                null
            )
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("Handled {StatusCode} on {Method} {Path}: {Message}", statusCode, context.Request.Method, context.Request.Path, title);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = statusCode,
            message = title,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

