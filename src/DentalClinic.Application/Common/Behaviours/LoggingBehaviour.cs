using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Logging;

using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest>(ILogger<TRequest> logger, IUser user, IIdentityService identityService)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger = logger;
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;

    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _user.Id ?? string.Empty;
        string? userName = string.Empty;

        if (!string.IsNullOrEmpty(userId))
        {
            userName = await _identityService.GetUserNameAsync(userId);
        }

        // The request is logged in a redacted form: a password, a refresh token
        // or any other credential must never reach the log sink.
        _logger.LogInformation(
            "Request: {Name} {UserId} {UserName} {Request}",
            requestName,
            userId,
            userName,
            SensitiveDataRedactor.Describe(request));
    }
}