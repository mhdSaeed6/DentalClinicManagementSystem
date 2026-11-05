using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Mappers;
using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Clinics.Queries.GetExchangeRate;

public class GetExchangeRateQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetExchangeRateQuery, Result<ExchangeRateDto>>
{
    public async Task<Result<ExchangeRateDto>> Handle(
        GetExchangeRateQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await context.ClinicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return ApplicationErrors.ClinicSettingsNotConfigured;
        }

        var secondaryCurrency = settings.SecondaryCurrency;

        // No secondary currency means nothing to convert: the caller hides the
        // secondary amount rather than showing a made-up one.
        if (string.IsNullOrWhiteSpace(secondaryCurrency))
        {
            return ApplicationErrors.ExchangeRateNotConfigured;
        }

        // The history for a single pair is a handful of rows, so the selection
        // rule stays in the domain (where it is unit-tested) instead of being
        // re-expressed as SQL here.
        var rates = await context.ExchangeRates
            .AsNoTracking()
            .Where(rate => rate.FromCurrency == settings.BaseCurrency
                        && rate.ToCurrency == secondaryCurrency)
            .ToListAsync(cancellationToken);

        var asOfUtc = request.AsOfUtc ?? timeProvider.GetUtcNow();
        var applicable = ExchangeRate.PickLatest(rates, asOfUtc);

        if (applicable is null)
        {
            return ApplicationErrors.ExchangeRateNotConfigured;
        }

        return applicable.ToDto();
    }
}