using System.Security.Claims;

using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.Features.Identity.Queries.RefreshTokens;
using DentalClinic.Domain.Common.Results;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class RefreshTokenQueryHandlerTests
{
    private const string UserId = "d0c7a5b2-3f61-4c8e-9a2d-1b2c3d4e5f60";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IAppDbContext _context = Substitute.For<IAppDbContext>();
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();

    private readonly RefreshTokenQueryHandler _sut;

    public RefreshTokenQueryHandlerTests()
    {
        _tokenProvider.GetPrincipalFromExpiredToken(Arg.Any<string>())
            .Returns(Principal(UserId));

        _sut = new RefreshTokenQueryHandler(
            NullLogger<RefreshTokenQueryHandler>.Instance,
            _identityService,
            _context,
            _tokenProvider);
    }

    [Fact]
    public async Task Handle_WhenUserNoLongerExists_ReturnsUniformInvalidTokenError()
    {
        // A token for a deleted account must not surface as a 404: that would let
        // a caller distinguish "account existed" from "token was forged".
        _identityService.GetUserByIdAsync(UserId).Returns(ApplicationErrors.UserNotFound);

        var result = await _sut.Handle(
            new RefreshTokenQuery("raw-refresh-token", "expired-access-token"),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.RefreshTokenExpired.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WhenUserNoLongerExists_DoesNotQueryRefreshTokens()
    {
        _identityService.GetUserByIdAsync(UserId).Returns(ApplicationErrors.UserNotFound);

        await _sut.Handle(
            new RefreshTokenQuery("raw-refresh-token", "expired-access-token"),
            CancellationToken.None);

        _ = _context.DidNotReceive().RefreshTokens;
    }

    [Fact]
    public async Task Handle_WhenIdentityReturnsAnotherError_PropagatesIt()
    {
        var unexpected = Error.Failure("Auth.Unexpected", "Something else broke.");
        _identityService.GetUserByIdAsync(UserId).Returns(unexpected);

        var result = await _sut.Handle(
            new RefreshTokenQuery("raw-refresh-token", "expired-access-token"),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(unexpected.Code, result.TopError.Code);
    }

    private static ClaimsPrincipal Principal(string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}