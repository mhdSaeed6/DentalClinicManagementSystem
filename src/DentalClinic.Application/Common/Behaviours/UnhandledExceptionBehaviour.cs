using DentalClinic.Application.Common.Logging;

using MediatR;

using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Common.Behaviours;

public class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next(ct);
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            // The request is logged in a redacted form: a password, a refresh
            // token or any other credential must never reach the log sink.
            _logger.LogError(
                ex,
                "Request: Unhandled Exception for Request {Name} {Request}",
                requestName,
                SensitiveDataRedactor.Describe(request));

            throw;
        }
    }
}