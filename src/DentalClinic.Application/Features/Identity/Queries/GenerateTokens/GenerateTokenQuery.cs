using DentalClinic.Application.Features.Identity;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

public record GenerateTokenQuery(
    string Email,
    string Password) : IRequest<Result<TokenResponse>>;