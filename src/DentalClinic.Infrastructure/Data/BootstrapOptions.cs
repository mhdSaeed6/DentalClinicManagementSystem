using Microsoft.Extensions.Configuration;

namespace DentalClinic.Infrastructure.Data;

/// <summary>
/// The values needed to bootstrap a fresh production deployment: the first
/// administrator and the clinic profile. Everything is optional at the type
/// level so <c>--bootstrap-admin</c> can validate the required pair
/// (<see cref="AdminEmail"/> / <see cref="AdminPassword"/>) and fail with a
/// message that names the missing environment variable, instead of an opaque
/// null reference.
/// </summary>
/// <remarks>
/// Bound from the <c>Bootstrap</c> configuration section, which in a container
/// is supplied as <c>Bootstrap__AdminEmail</c>, <c>Bootstrap__AdminPassword</c>,
/// <c>Bootstrap__ClinicName</c>, <c>Bootstrap__ClinicNameArabic</c>,
/// <c>Bootstrap__TimeZoneId</c>, <c>Bootstrap__BaseCurrency</c> and
/// <c>Bootstrap__DefaultCountryCode</c>. These variables should be removed from
/// the environment after the first successful bootstrap.
/// </remarks>
public sealed class BootstrapOptions
{
    public const string DefaultClinicName = "Al Noor Dental Clinic";
    public const string DefaultClinicNameArabic = "عيادة النور لطب الأسنان";
    public const string DefaultTimeZoneId = "Asia/Damascus";
    public const string DefaultBaseCurrency = "USD";
    public const string DefaultDefaultCountryCode = "963";

    /// <summary>E-mail of the first administrator. Required by <c>--bootstrap-admin</c>.</summary>
    public string? AdminEmail { get; init; }

    /// <summary>Password of the first administrator. Required by <c>--bootstrap-admin</c>.</summary>
    public string? AdminPassword { get; init; }

    public string? ClinicName { get; init; }

    public string? ClinicNameArabic { get; init; }

    public string? TimeZoneId { get; init; }

    public string? BaseCurrency { get; init; }

    public string? DefaultCountryCode { get; init; }

    public string ResolvedClinicName => Clean(ClinicName) ?? DefaultClinicName;

    public string ResolvedClinicNameArabic => Clean(ClinicNameArabic) ?? DefaultClinicNameArabic;

    public string ResolvedTimeZoneId => Clean(TimeZoneId) ?? DefaultTimeZoneId;

    public string ResolvedBaseCurrency => Clean(BaseCurrency) ?? DefaultBaseCurrency;

    public string ResolvedDefaultCountryCode => Clean(DefaultCountryCode) ?? DefaultDefaultCountryCode;

    public static BootstrapOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new BootstrapOptions
        {
            AdminEmail = configuration["Bootstrap:AdminEmail"],
            AdminPassword = configuration["Bootstrap:AdminPassword"],
            ClinicName = configuration["Bootstrap:ClinicName"],
            ClinicNameArabic = configuration["Bootstrap:ClinicNameArabic"],
            TimeZoneId = configuration["Bootstrap:TimeZoneId"],
            BaseCurrency = configuration["Bootstrap:BaseCurrency"],
            DefaultCountryCode = configuration["Bootstrap:DefaultCountryCode"],
        };
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}