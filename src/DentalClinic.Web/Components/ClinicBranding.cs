using DentalClinic.Web.Api;

using Microsoft.Extensions.Configuration;

namespace DentalClinic.Web.Components;

/// <summary>
/// The clinic's identity as it appears on screen and on printed documents.
/// </summary>
/// <remarks>
/// <para>
/// Values come from three places, in order: the server-persisted clinic profile
/// (<see cref="IClinicSettingsSource"/>), the legacy <c>Clinic</c> configuration
/// section, and the built-in defaults. The configuration path survives so a
/// deployment can brand its paperwork before the settings row exists, and so a
/// unit test can exercise the fallback without HTTP.
/// </para>
/// <para>
/// The properties are synchronous on purpose — every print sheet and layout
/// reads them directly — so the profile is loaded once by
/// <see cref="EnsureLoadedAsync"/> and cached for the lifetime of the scope.
/// </para>
/// </remarks>
public sealed class ClinicBranding(IConfiguration configuration, IClinicSettingsSource settingsSource)
{
    private const string DefaultName = "Al Noor Dental Clinic";
    private const string DefaultNameArabic = "عيادة النور لطب الأسنان";

    private readonly IConfiguration _configuration = configuration;
    private readonly IClinicSettingsSource _settingsSource = settingsSource;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ClinicSettingsDto? _settings;

    public string Name => _settings is not null ? _settings.Name : Value("Clinic:Name") ?? DefaultName;

    public string NameArabic => _settings is not null ? _settings.NameArabic : Value("Clinic:NameArabic") ?? DefaultNameArabic;

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Address => _settings is not null ? _settings.Address : Value("Clinic:Address");

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Phone => _settings is not null ? _settings.Phone : Value("Clinic:Phone");

    /// <summary>Optional: omitted from the sheet when not configured.</summary>
    public string? Email => _settings is not null ? _settings.Email : Value("Clinic:Email");

    /// <summary>Optional: relative path served by the API, e.g. <c>/branding/logo.svg</c>.</summary>
    public string? LogoUrl => _settings?.LogoUrl;

    public string TimeZoneId => _settings?.TimeZoneId ?? "UTC";

    /// <summary>
    /// The clinic's currency code. Empty until the profile loads: money then
    /// renders as a bare number rather than inventing a currency the clinic
    /// never configured.
    /// </summary>
    public string BaseCurrency => _settings is not null
        ? _settings.BaseCurrency
        : Value("Clinic:BaseCurrency") ?? string.Empty;

    public string? SecondaryCurrency => _settings?.SecondaryCurrency;

    public string DisplayName(bool arabic) => arabic ? NameArabic : Name;

    /// <summary>
    /// Loads the profile once. Failures are silent and leave the configuration
    /// fallback in place — branding is never allowed to break a page.
    /// </summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_settings is not null)
        {
            return;
        }

        await _gate.WaitAsync(ct);

        try
        {
            if (_settings is not null)
            {
                return;
            }

            _settings = await _settingsSource.GetClinicSettingsAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Forces a refetch, e.g. after the settings page saves a change.</summary>
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        _settings = null;
        await EnsureLoadedAsync(ct);
    }

    private string? Value(string key)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}