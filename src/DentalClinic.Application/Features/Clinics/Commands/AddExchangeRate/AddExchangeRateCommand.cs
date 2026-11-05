using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Clinics.Commands.AddExchangeRate;

/// <summary>
/// Appends a rate to the history. There is deliberately no update or delete
/// command: a correction is a new row with a later effective date, so an
/// already-printed invoice can always be explained.
/// </summary>
public sealed record AddExchangeRateCommand(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTimeOffset? EffectiveFromUtc) : IRequest<Result<ExchangeRateDto>>;