using DentalClinic.Application.Common.Behaviours;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Logging;

using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DentalClinic.Application.UnitTests.Behaviours;

public class LoggingBehaviourTests
{
    private readonly ILogger<DummyRequest> _logger = Substitute.For<ILogger<DummyRequest>>();
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    private readonly LoggingBehaviour<DummyRequest> _sut;

    public LoggingBehaviourTests()
    {
        _sut = new LoggingBehaviour<DummyRequest>(_logger, _user, _identityService);
    }

    [Fact]
    public async Task Process_WithUserId_LogsRequestWithUserName()
    {
        // Arrange
        var request = new DummyRequest();
        _user.Id.Returns("abc123");
        _identityService.GetUserNameAsync("abc123").Returns("Issam");

        // Act
        await _sut.Process(request, CancellationToken.None);

        // Assert
        await _identityService.Received(1).GetUserNameAsync("abc123");

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Request")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Process_WithoutUserId_LogsRequestWithEmptyUserName()
    {
        // Arrange
        var request = new DummyRequest();
        _user.Id.Returns((string?)null);

        // Act
        await _sut.Process(request, CancellationToken.None);

        // Assert
        await _identityService.DidNotReceive().GetUserNameAsync(Arg.Any<string>());

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Request")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Process_WithSensitiveRequest_NeverLogsTheSecret()
    {
        // Arrange
        const string password = "Admin!Pass123";
        const string refreshToken = "refresh-token-value-that-must-not-be-logged";
        var request = new DummyRequest
        {
            Password = password,
            RefreshToken = refreshToken
        };
        _user.Id.Returns("abc123");
        _identityService.GetUserNameAsync("abc123").Returns("Issam");

        // Act
        await _sut.Process(request, CancellationToken.None);

        // Assert - the password must not appear anywhere in the log entry.
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o =>
                !o.ToString()!.Contains(password) &&
                !o.ToString()!.Contains(refreshToken) &&
                o.ToString()!.Contains(SensitiveDataRedactor.RedactedValue)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    public class DummyRequest
    {
        public string Password { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}