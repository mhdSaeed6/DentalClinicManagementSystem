namespace DentalClinic.Infrastructure.Identity;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Forces JWT secret resolution once the host has started, so a missing secret
/// outside Development fails at startup instead of on the first authenticated
/// request.
/// </summary>
public sealed class JwtSecretValidationService(IJwtSecretProvider secretProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = secretProvider.GetSecret();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}