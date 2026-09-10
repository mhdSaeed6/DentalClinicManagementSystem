using System.Security.Claims;

using DentalClinic.Application.Features.Identity;

using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}