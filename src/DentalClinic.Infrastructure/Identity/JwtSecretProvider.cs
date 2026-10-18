namespace DentalClinic.Infrastructure.Identity;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Resolves the JWT signing secret lazily from the fully-built configuration.
/// </summary>
/// <remarks>
/// This must not be read during service registration. <c>WebApplicationFactory</c>
/// queues its <c>ConfigureAppConfiguration</c> callbacks and applies them while the
/// host is built, so a value read eagerly inside <c>Program.Main</c> would not yet
/// include anything the test host (or a late configuration source) supplied.
/// </remarks>
public interface IJwtSecretProvider
{
    /// <summary>
    /// The configured secret, or an ephemeral one in Development.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown outside Development when no secret is configured. Fails loudly
    /// rather than signing tokens with a well-known default.
    /// </exception>
    string GetSecret();
}

/// <inheritdoc cref="IJwtSecretProvider" />
public sealed class JwtSecretProvider(IConfiguration configuration) : IJwtSecretProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly Lock _gate = new();
    private string? _cached;

    public string GetSecret()
    {
        // Cached so the Development fallback is stable for the process lifetime;
        // a new key per call would invalidate tokens on every request.
        if (_cached is not null)
        {
            return _cached;
        }

        lock (_gate)
        {
            _cached ??= Resolve();
        }

        return _cached;
    }

    private string Resolve()
    {
        var configured = _configuration["JwtSettings:Secret"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var isDevelopment = string.Equals(
            _configuration["ASPNETCORE_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. Set it via the JwtSettings__Secret " +
                "environment variable, user-secrets, or your secret manager. Refusing to start.");
        }

        var ephemeral = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

        // Back-fill so other components reading configuration directly see the
        // same value.
        _configuration["JwtSettings:Secret"] = ephemeral;

        return ephemeral;
    }
}