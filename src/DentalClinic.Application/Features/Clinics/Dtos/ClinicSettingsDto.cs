namespace DentalClinic.Application.Features.Clinics.Dtos;

/// <summary>
/// Public projection of the clinic profile. Anonymous callers read it (the login
/// page and print sheets need branding before anyone signs in), so it must never
/// grow a field that is not safe to expose.
/// </summary>
public sealed record ClinicSettingsDto(
    string Name,
    string NameArabic,
    string? LogoUrl,
    string? Phone,
    string? Address,
    string? Email,
    string TimeZoneId,
    string BaseCurrency,
    string? SecondaryCurrency,
    string? DefaultCountryCode);