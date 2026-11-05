using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

/// <summary>
/// Supplies the clinic profile to <see cref="ClinicBranding"/>. Kept as a tiny
/// interface so branding can be unit-tested without an HTTP stack, and so the
/// Web project never takes a dependency on the API's application layer.
/// </summary>
public interface IClinicSettingsSource
{
    /// <summary>
    /// Returns the clinic profile, or <see langword="null"/> when it cannot be
    /// read. Branding must never fail a page render, so transport errors are
    /// swallowed here rather than propagated.
    /// </summary>
    Task<ClinicSettingsDto?> GetClinicSettingsAsync(CancellationToken ct = default);
}