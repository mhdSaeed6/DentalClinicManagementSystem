using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Mappers;
using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Clinics.Commands.AddExchangeRate;

public class AddExchangeRateCommandHandler(
    ILogger<AddExchangeRateCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    TimeProvider timeProvider)
    : IRequestHandler<AddExchangeRateCommand, Result<ExchangeRateDto>>
{
    public async Task<Result<ExchangeRateDto>> Handle(
        AddExchangeRateCommand request,
        CancellationToken cancellationToken)
    {
        var effectiveFromUtc = request.EffectiveFromUtc ?? timeProvider.GetUtcNow();

        var createResult = ExchangeRate.Create(
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            effectiveFromUtc);

        if (createResult.IsError)
        {
            logger.LogWarning(
                "Exchange rate rejected: {@Errors}",
                createResult.Errors);

            return createResult.Errors;
        }

        var rate = createResult.Value;

        context.ExchangeRates.Add(rate);

        await context.SaveChangesAsync(cancellationToken);

        // Every cached conversion is now potentially stale.
        await cache.RemoveByTagAsync("exchange-rate", cancellationToken);

        logger.LogInformation(
            "Exchange rate appended: 1 {FromCurrency} = {Rate} {ToCurrency} effective {EffectiveFromUtc:o}.",
            rate.FromCurrency,
            rate.Rate,
            rate.ToCurrency,
            rate.EffectiveFromUtc);

        return rate.ToDto();
    }
}