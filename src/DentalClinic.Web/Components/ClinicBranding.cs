using Microsoft.Extensions.Configuration;

namespace DentalClinic.Web.Components;

/// <summary>
/// The clinic's identity as it appears on printed documents.
/// </summary>
/// <remarks>
/// This is the seam for P2.2 (server-persisted clinic profile): today the values
/// come from the <c>Clinic</c> configuration section with a sensible fallback,
/// so a deployment can already brand its paperwork without a code change. When
/// the settings slice lands, only this class changes — every print sheet keeps
/// asking <see cref="ClinicBranding"/> for the name, address and phone.
///
/// Values are read lazily on access (never captured during registration) so a
/// late configuration source is still observed.
/// </remarks>
public sealed class ClinicBranding(IConfiguration configuration)
{
    private const string DefaultName = "Al Noor Dental Clinic";
    private const string DefaultNameArabic = "عيادة النور لطب الأسنان";

    private readonly IConfiguration _configuration = configuration;

    public string Name => Value("Clinic:Name") ?? DefaultName;

    public string NameArabic => Value("Clinic:NameArabic") ?? DefaultNameArabic;

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Address => Value("Clinic:Address");

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Phone => Value("Clinic:Phone");

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Email => Value("Clinic:Email");

    public string DisplayName(bool arabic) => arabic ? NameArabic : Name;

    private string? Value(string key)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}