namespace DentalClinic.Application.Features.Clinics.Dtos;

/// <summary>
/// The conversion rate in force at the requested instant. <see cref="Rate"/>
/// reads as "one unit of <see cref="FromCurrency"/> = <see cref="Rate"/> units
/// of <see cref="ToCurrency"/>".
/// </summary>
public sealed record ExchangeRateDto(
    Guid Id,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTimeOffset EffectiveFromUtc);