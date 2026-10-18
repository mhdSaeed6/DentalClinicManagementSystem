namespace DentalClinic.Api;

/// <summary>
/// Names of the rate-limiter policies registered in
/// <c>AddAppRateLimiting</c> and applied per-endpoint via
/// <c>[EnableRateLimiting]</c>.
///
/// These live in their own type rather than on <c>DependencyInjection</c>
/// because both this project and DentalClinic.Application declare a
/// <c>DependencyInjection</c> class, which makes the qualified name ambiguous
/// (CS0436) from controllers.
/// </summary>
public static class RateLimiterPolicyNames
{
    /// <summary>General-purpose sliding window limiter.</summary>
    public const string SlidingWindow = "SlidingWindow";

    /// <summary>Tight limiter for credential endpoints (token issue/refresh).</summary>
    public const string Auth = "Auth";
}