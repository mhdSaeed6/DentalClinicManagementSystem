using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Clinics;

namespace DentalClinic.Application.Features.Clinics.Mappers;

public static class ExchangeRateMappingExtensions
{
    public static ExchangeRateDto ToDto(this ExchangeRate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ExchangeRateDto(
            entity.Id,
            entity.FromCurrency,
            entity.ToCurrency,
            entity.Rate,
            entity.EffectiveFromUtc);
    }
}