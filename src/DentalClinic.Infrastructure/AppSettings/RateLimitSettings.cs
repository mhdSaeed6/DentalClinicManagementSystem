namespace DentalClinic.Infrastructure.Settings;

/// <summary>
/// Rate-limiter tuning, bound from the <c>AppSettings:RateLimiting</c> section.
/// Defaults are deliberately strict; tests override them to avoid the suite
/// throttling itself.
/// </summary>
public class RateLimitSettings
{
    /// <summary>Permitted token requests per window, per client.</summary>
    public int AuthPermitLimit { get; set; } = 10;

    /// <summary>Token-request window length, in minutes.</summary>
    public int AuthWindowMinutes { get; set; } = 1;

    /// <summary>General-purpose limiter permit count.</summary>
    public int GeneralPermitLimit { get; set; } = 100;

    /// <summary>General-purpose limiter window length, in minutes.</summary>
    public int GeneralWindowMinutes { get; set; } = 1;
}