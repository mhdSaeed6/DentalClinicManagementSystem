using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Clinics;

public static class ExchangeRateErrors
{
    public static readonly Error InvalidCurrency = Error.Validation(
        "ExchangeRate.InvalidCurrency",
        "Both currencies must be three-letter ISO-4217 codes.");

    public static readonly Error SameCurrency = Error.Validation(
        "ExchangeRate.SameCurrency",
        "The source and target currencies must differ.");

    public static readonly Error InvalidRate = Error.Validation(
        "ExchangeRate.InvalidRate",
        "The rate must be greater than zero.");

    public static readonly Error InvalidEffectiveDate = Error.Validation(
        "ExchangeRate.InvalidEffectiveDate",
        "An effective date is required.");
}