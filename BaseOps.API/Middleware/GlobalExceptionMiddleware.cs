using System.Net;
using FluentValidation;
using System.Text.Json;

namespace BaseOps.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new { message = "Validation error", errors = ex.Errors.Select(e => e.ErrorMessage), correlationId = context.TraceIdentifier });
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message, correlationId = context.TraceIdentifier });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for correlation {CorrelationId}: {Message}", context.TraceIdentifier, ex.Message);
            logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred.", correlationId = context.TraceIdentifier, details = ex.Message }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
