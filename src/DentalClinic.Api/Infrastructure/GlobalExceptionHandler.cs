using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace DentalClinic.Api.Infrastructure;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Message returned to the caller outside Development. It must never carry
    /// the exception type or its message: both describe the internals of the
    /// application and are only useful to an attacker probing the API.
    /// </summary>
    private const string GenericDetail = "An unexpected error occurred. Please try again later.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // The full failure is always recorded server-side; only the response
        // body is stripped of internal detail outside Development.
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var isDevelopment = environment.IsDevelopment();

        var problemDetails = new ProblemDetails
        {
            Title = "Application error",
            Detail = isDevelopment ? exception.Message : GenericDetail,
            Status = StatusCodes.Status500InternalServerError,
        };

        if (isDevelopment)
        {
            problemDetails.Type = exception.GetType().Name;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
