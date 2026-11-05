using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Clinics.Queries.GetExchangeRate;

/// <summary>
/// The conversion rate for the clinic's configured currency pair, at a given
/// instant (or now when no instant is supplied). The print sheets ask for the
/// rate that applied when the invoice was issued; the billing screens ask for
/// the current one.
/// </summary>
/// <remarks>
/// Tagged with both <c>exchange-rate</c> and <c>clinic-setting</c>: appending a
/// rate busts the first, and changing the currency pair busts the second, so a
/// cached answer can never describe a pair the clinic no longer uses.
/// </remarks>
public sealed record GetExchangeRateQuery(DateTimeOffset? AsOfUtc = null)
    : ICachedQuery<Result<ExchangeRateDto>>
{
    public string CacheKey => AsOfUtc is null
        ? "exchange-rate-current"
        : $"exchange-rate-{AsOfUtc.Value.UtcTicks}";

    public string[] Tags => ["exchange-rate", "clinic-setting"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}