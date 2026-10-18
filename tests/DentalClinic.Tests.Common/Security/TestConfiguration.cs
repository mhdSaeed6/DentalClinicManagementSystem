namespace DentalClinic.Tests.Common.Security;

/// <summary>
/// Configuration values injected into <c>WebApplicationFactory</c> so the API
/// boots under test.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// The API refuses to start outside Development when no JWT signing secret is
    /// configured. <c>WebApplicationFactory</c> does not set
    /// ASPNETCORE_ENVIRONMENT, so the host runs as Production and must be given
    /// a secret explicitly, exactly as a real deployment provides one.
    /// </summary>
    public const string JwtSecret = "integration-tests-only-jwt-secret-do-not-use-anywhere-else-0123456789";

    /// <summary>
    /// The suite issues a token per request, so the credential-endpoint limiter
    /// is raised well above what a human would tolerate, to avoid self-throttling.
    /// </summary>
    public const int AuthPermitLimit = 100_000;
}
