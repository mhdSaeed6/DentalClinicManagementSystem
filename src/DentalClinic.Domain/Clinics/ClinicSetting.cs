using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Clinics;

/// <summary>
/// The clinic's own identity: what it is called, how it is reached, and the
/// regional settings every other feature reads (timezone, currency).
/// </summary>
/// <remarks>
/// This is a singleton aggregate — exactly one row exists, inserted by the
/// migration that creates the table and addressed through
/// <see cref="SingletonId"/>. Modelling it as an entity (rather than a
/// key/value settings table) keeps every field typed and lets the domain
/// validate currency and timezone in one place.
/// </remarks>
public sealed class ClinicSetting : AuditableEntity
{
    /// <summary>The one and only row id. Stable so a re-seed never duplicates it.</summary>
    public static readonly Guid SingletonId = Guid.Parse("c11c0000-0000-4000-8000-000000000001");

    public string Name { get; private set; } = default!;
    public string NameArabic { get; private set; } = default!;

    /// <summary>Optional logo path served by the API's static files, e.g. <c>/branding/logo.svg</c>.</summary>
    public string? LogoUrl { get; private set; }

    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Email { get; private set; }

    /// <summary>IANA or Windows timezone id. Day buckets and invoice years use it.</summary>
    public string TimeZoneId { get; private set; } = default!;

    /// <summary>ISO-4217 code the money columns are stored in.</summary>
    public string BaseCurrency { get; private set; } = default!;

    /// <summary>Optional second currency used for display only, e.g. a local currency beside USD.</summary>
    public string? SecondaryCurrency { get; private set; }

    /// <summary>Dialling prefix for <c>wa.me</c> links, e.g. <c>20</c>.</summary>
    public string? DefaultCountryCode { get; private set; }

    private ClinicSetting()
    {
    }

    private ClinicSetting(
        Guid id,
        string name,
        string nameArabic,
        string? logoUrl,
        string? phone,
        string? address,
        string? email,
        string timeZoneId,
        string baseCurrency,
        string? secondaryCurrency,
        string? defaultCountryCode)
        : base(id)
    {
        Name = name;
        NameArabic = nameArabic;
        LogoUrl = logoUrl;
        Phone = phone;
        Address = address;
        Email = email;
        TimeZoneId = timeZoneId;
        BaseCurrency = baseCurrency;
        SecondaryCurrency = secondaryCurrency;
        DefaultCountryCode = defaultCountryCode;
    }

    public static Result<ClinicSetting> Create(
        string name,
        string nameArabic,
        string? logoUrl,
        string? phone,
        string? address,
        string? email,
        string timeZoneId,
        string baseCurrency,
        string? secondaryCurrency,
        string? defaultCountryCode)
    {
        var validation = Validate(name, nameArabic, timeZoneId, baseCurrency, secondaryCurrency);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new ClinicSetting(
            SingletonId,
            name.Trim(),
            nameArabic.Trim(),
            Clean(logoUrl),
            Clean(phone),
            Clean(address),
            Clean(email),
            timeZoneId.Trim(),
            baseCurrency.Trim().ToUpperInvariant(),
            Clean(secondaryCurrency)?.ToUpperInvariant(),
            Clean(defaultCountryCode));
    }

    public Result<Updated> Update(
        string name,
        string nameArabic,
        string? logoUrl,
        string? phone,
        string? address,
        string? email,
        string timeZoneId,
        string baseCurrency,
        string? secondaryCurrency,
        string? defaultCountryCode)
    {
        var validation = Validate(name, nameArabic, timeZoneId, baseCurrency, secondaryCurrency);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        Name = name.Trim();
        NameArabic = nameArabic.Trim();
        LogoUrl = Clean(logoUrl);
        Phone = Clean(phone);
        Address = Clean(address);
        Email = Clean(email);
        TimeZoneId = timeZoneId.Trim();
        BaseCurrency = baseCurrency.Trim().ToUpperInvariant();
        SecondaryCurrency = Clean(secondaryCurrency)?.ToUpperInvariant();
        DefaultCountryCode = Clean(defaultCountryCode);

        return Result.Updated;
    }

    /// <summary>
    /// Resolves <see cref="TimeZoneId"/> without ever throwing: a typo must not
    /// stop billing, so an unknown id falls back to the host timezone.
    /// </summary>
    public TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static Result<Success> Validate(
        string name,
        string nameArabic,
        string timeZoneId,
        string baseCurrency,
        string? secondaryCurrency)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(nameArabic))
        {
            return ClinicSettingErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(timeZoneId) || !IsKnownTimeZone(timeZoneId))
        {
            return ClinicSettingErrors.InvalidTimeZone;
        }

        if (!IsCurrencyCode(baseCurrency))
        {
            return ClinicSettingErrors.InvalidBaseCurrency;
        }

        if (!string.IsNullOrWhiteSpace(secondaryCurrency))
        {
            if (!IsCurrencyCode(secondaryCurrency))
            {
                return ClinicSettingErrors.InvalidSecondaryCurrency;
            }

            if (string.Equals(baseCurrency.Trim(), secondaryCurrency.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ClinicSettingErrors.SecondaryCurrencyMatchesBase;
            }
        }

        return Result.Success;
    }

    private static bool IsKnownTimeZone(string id)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id.Trim());

            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsCurrencyCode(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Trim().Length == 3
           && value.Trim().All(char.IsLetter);

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}