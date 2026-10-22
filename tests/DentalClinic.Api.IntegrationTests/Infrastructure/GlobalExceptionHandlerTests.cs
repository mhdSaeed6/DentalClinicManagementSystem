using DentalClinic.Api.Infrastructure;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Infrastructure;

/// <summary>
/// The global exception handler is the last line of defence: outside
/// Development it must not describe the internals of the application to the
/// caller, while the failure itself is still recorded server-side.
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_OutsideDevelopment_ReturnsGenericMessageWithoutExceptionDetails()
    {
        // Arrange
        var problemDetailsService = new RecordingProblemDetailsService();
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(
            problemDetailsService,
            new TestHostEnvironment { EnvironmentName = Environments.Production },
            logger);

        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Login failed for user admin@hospital with password hunter2.");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);

        var problemDetails = problemDetailsService.Context!.ProblemDetails;

        Assert.Equal("Application error", problemDetails.Title);
        Assert.DoesNotContain("hunter2", problemDetails.Detail ?? string.Empty);
        Assert.DoesNotContain(nameof(InvalidOperationException), problemDetails.Type ?? string.Empty);
        Assert.Null(problemDetails.Type);

        // The failure itself must still be recorded server-side.
        Assert.Contains(logger.Entries, entry => entry.Exception == exception);
    }

    [Fact]
    public async Task TryHandleAsync_InDevelopment_KeepsTheExceptionDetails()
    {
        // Arrange
        var problemDetailsService = new RecordingProblemDetailsService();
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(
            problemDetailsService,
            new TestHostEnvironment { EnvironmentName = Environments.Development },
            logger);

        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("the real reason");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);

        var problemDetails = problemDetailsService.Context!.ProblemDetails;

        Assert.Equal("the real reason", problemDetails.Detail);
        Assert.Equal(nameof(InvalidOperationException), problemDetails.Type);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext problemDetailsContext)
        {
            Context = problemDetailsContext;

            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext problemDetailsContext)
        {
            Context = problemDetailsContext;

            return ValueTask.FromResult(true);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "DentalClinic.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }
}
