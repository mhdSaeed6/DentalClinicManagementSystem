using DentalClinic.Application.Common.Logging;
using DentalClinic.Application.Features.Identity.Queries.GenerateTokens;
using DentalClinic.Application.Features.Identity.Queries.RefreshTokens;

using Xunit;

namespace DentalClinic.Application.UnitTests.Common;

public class SensitiveDataRedactorTests
{
    [Fact]
    public void Describe_WithTokenRequest_RedactsThePassword()
    {
        // Arrange
        var request = new GenerateTokenQuery("admin@dentalclinic.com", "Admin!Pass123");

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.DoesNotContain("Admin!Pass123", described);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, described);
        Assert.Contains("admin@dentalclinic.com", described);
    }

    [Fact]
    public void Describe_WithRefreshTokenRequest_RedactsBothTokens()
    {
        // Arrange
        var request = new RefreshTokenQuery("the-raw-refresh-token", "the-expired-access-token");

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.DoesNotContain("the-raw-refresh-token", described);
        Assert.DoesNotContain("the-expired-access-token", described);
    }

    [Fact]
    public void Describe_WithNestedSecret_RedactsAtAnyDepth()
    {
        // Arrange
        var request = new OuterRequest
        {
            Name = "Ada",
            Inner = new InnerRequest { Password = "Nested!Secret123", Note = "a visible note" },
        };

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.DoesNotContain("Nested!Secret123", described);
        Assert.Contains("a visible note", described);
        Assert.Contains("Ada", described);
    }

    [Fact]
    public void Describe_WithJwtShapedValue_RedactsItEvenUnderAnInnocentName()
    {
        // Arrange
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dBjftJeZ4CVPmB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var request = new OuterRequest { Name = jwt };

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.DoesNotContain(jwt, described);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, described);
    }

    [Fact]
    public void Describe_WithOpaqueCredential_RedactsItEvenUnderAnInnocentName()
    {
        // Arrange - the shape of a raw refresh token (random base64).
        const string opaque = "aB3dEfGh1JkL2mN4pQ6rStU8vWxYz0AbCdEfGhIjKl";
        var request = new OuterRequest { Name = opaque };

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.DoesNotContain(opaque, described);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, described);
    }

    [Fact]
    public void Describe_WithOrdinaryValues_KeepsThemReadable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new OrdinaryRequest
        {
            Id = id,
            Amount = 42.5m,
            Note = "Composite filling on tooth 11",
        };

        // Act
        var described = SensitiveDataRedactor.Describe(request);

        // Assert
        Assert.Contains(id.ToString(), described);
        Assert.Contains("Composite filling on tooth 11", described);
        Assert.Contains("42.5", described);
    }

    [Fact]
    public void Describe_WithNullRequest_ReturnsEmptyJson()
    {
        Assert.Equal("{}", SensitiveDataRedactor.Describe(null));
    }

    [Theory]
    [InlineData("Password", true)]
    [InlineData("password", true)]
    [InlineData("RefreshToken", true)]
    [InlineData("ExpiredAccessToken", true)]
    [InlineData("ClientSecret", true)]
    [InlineData("Authorization", true)]
    [InlineData("Email", false)]
    [InlineData("PatientId", false)]
    [InlineData("Amount", false)]
    [InlineData("Description", false)]
    public void IsSensitiveName_MatchesSecretLikeNamesOnly(string name, bool expected)
    {
        Assert.Equal(expected, SensitiveDataRedactor.IsSensitiveName(name));
    }

    [Fact]
    public void IsSecretValue_WithGuid_ReturnsFalse()
    {
        // A GUID is not a credential and must stay readable in the logs.
        Assert.False(SensitiveDataRedactor.IsSecretValue(Guid.NewGuid().ToString()));
    }

    private sealed class OuterRequest
    {
        public string Name { get; init; } = string.Empty;

        public InnerRequest? Inner { get; init; }
    }

    private sealed class InnerRequest
    {
        public string Password { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;
    }

    private sealed class OrdinaryRequest
    {
        public Guid Id { get; init; }

        public decimal Amount { get; init; }

        public string Note { get; init; } = string.Empty;
    }
}
