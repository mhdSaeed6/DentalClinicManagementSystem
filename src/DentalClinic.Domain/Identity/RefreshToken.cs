using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Identity;

public sealed class RefreshToken : AuditableEntity
{
    public string? TokenHash { get; }
    public string? UserId { get; }
    public DateTimeOffset ExpiresOnUtc { get; }

    private RefreshToken()
    { }

    private RefreshToken(Guid id, string? tokenHash, string? userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RefreshTokenErrors.UserIdRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, HashToken(token), userId, expiresOnUtc);
    }

    /// <summary>
    /// Hash of a raw token. Tokens are 256 bits of CSPRNG output, so there is no
    /// low-entropy secret to brute-force and a fast hash keeps lookup cheap.
    /// </summary>
    public static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }
}