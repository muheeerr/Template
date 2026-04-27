using System.Net;
using System.Text.Json;
using Template.Modules.Common.Contracts;
using FluentValidation;

namespace Template.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var payload = new ApiEnvelope<object?>(
                false,
                null,
                "Validation failed",
                exception.Errors
                    .Select(error => new ApiError(error.PropertyName, "validation", error.ErrorMessage))
                    .ToArray());

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ApiEnvelope<object?>(
                false,
                null,
                "An unexpected error occurred.",
                [new ApiError(null, "unexp" +
                "ected", "An unexpected error occurred.")]);

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
