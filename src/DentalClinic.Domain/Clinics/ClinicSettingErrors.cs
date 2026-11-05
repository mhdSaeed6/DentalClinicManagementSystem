using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Clinics;

public static class ClinicSettingErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "ClinicSetting.NameRequired",
        "Both the clinic name and its Arabic name are required.");

    public static readonly Error InvalidTimeZone = Error.Validation(
        "ClinicSetting.InvalidTimeZone",
        "The timezone id is not recognised by the host.");

    public static readonly Error InvalidBaseCurrency = Error.Validation(
        "ClinicSetting.InvalidBaseCurrency",
        "The base currency must be a three-letter ISO-4217 code.");

    public static readonly Error InvalidSecondaryCurrency = Error.Validation(
        "ClinicSetting.InvalidSecondaryCurrency",
        "The secondary currency must be a three-letter ISO-4217 code.");

    public static readonly Error SecondaryCurrencyMatchesBase = Error.Validation(
        "ClinicSetting.SecondaryCurrencyMatchesBase",
        "The secondary currency must differ from the base currency.");
}