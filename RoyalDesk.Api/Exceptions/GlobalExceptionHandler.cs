using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RoyalDesk.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Access denied.", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request.", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found.", exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Request could not be completed.", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "The request could not be completed.")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled error while processing {Path}", httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Request failed with {StatusCode} for {Path}", status, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        }, cancellationToken);

        return true;
    }
}
