using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DentalClinic.Infrastructure.Identity;

public class TokenProvider(IConfiguration configuration, IJwtSecretProvider secretProvider, IAppDbContext context) : ITokenProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IJwtSecretProvider _secretProvider = secretProvider;
    private readonly IAppDbContext _context = context;

    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default)
    {
        var tokenResult = await CreateAsync(user, ct);

        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        return tokenResult.Value;
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretProvider.GetSecret())),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = false, // Ignore token expiration
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            // التأكد أيضاً من أن التوكن تم توقيعه بنفس الخوارزمية (HMACSHA256)
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception)
        {
            // في حال كان التوكن مشوهاً (Malformed) أو غير صالح كلياً
            return null;
        }
    }

    private async Task<Result<TokenResponse>> CreateAsync(AppUserDto user, CancellationToken ct = default)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var key = _secretProvider.GetSecret();

        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpirationInMinutes"]!));

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.UserId!),
            new (JwtRegisteredClaimNames.Email, user.Email!),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        // Issue a trusted doctor_profile claim for doctor accounts so downstream
        // authorization does not have to re-resolve the profile on every request.
        if (user.Roles.Contains(Roles.Doctor))
        {
            var doctorId = await _context.Doctors
                .Where(d => d.UserId == user.UserId)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(ct);

            if (doctorId is { } id)
            {
                claims.Add(new(ClinicClaimTypes.DoctorId, id.ToString()));
            }
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var securityToken = tokenHandler.CreateToken(descriptor);

        // Retire the user's previous refresh tokens so a new sign-in invalidates
        // old sessions. Done as tracked deletes (the entity is soft-deletable)
        // rather than ExecuteDeleteAsync, which bypassed the global query filter
        // and raced concurrent sign-ins issuing tokens for the same user.
        var oldRefreshTokens = await _context.RefreshTokens
              .Where(rt => rt.UserId == user.UserId && !rt.IsDeleted)
              .ToListAsync(ct);

        if (oldRefreshTokens.Count > 0)
        {
            foreach (var stale in oldRefreshTokens)
            {
                stale.Delete(user.UserId);
            }
        }

        // The raw value is returned to the client exactly once here; only its
        // hash is persisted on the entity.
        var rawRefreshToken = GenerateRefreshToken();

        var refreshTokenResult = RefreshToken.Create(
            Guid.NewGuid(),
            rawRefreshToken,
            user.UserId,
            DateTime.UtcNow.AddDays(7));

        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }

        var refreshToken = refreshTokenResult.Value;

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(ct);

        return new TokenResponse
        {
            AccessToken = tokenHandler.WriteToken(securityToken),
            RefreshToken = rawRefreshToken,
            ExpiresOnUtc = expires
        };
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}