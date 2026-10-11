using DentalClinic.Application.Features.Appointments.Commands.MarkOverdueAppointmentsAsNoShow;
using DentalClinic.Infrastructure.Settings;

using MediatR;

using Microsoft.Extensions.Options;

namespace DentalClinic.Api.BackgroundServices;

/// <summary>
/// Periodically scans scheduled appointments whose slot has already passed and, after a grace
/// period, marks them as a no-show so staff can see who did not attend.
/// </summary>
public sealed class AppointmentNoShowBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AppSettings> options,
    ILogger<AppointmentNoShowBackgroundService> logger) : BackgroundService
{
    private readonly AppSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _settings.OverdueBookingCleanupFrequencyMinutes;
        if (intervalMinutes <= 0)
        {
            logger.LogInformation("Overdue appointment no-show cleanup is disabled.");
            return;
        }

        var gracePeriod = TimeSpan.FromMinutes(Math.Max(0, _settings.BookingCancellationThresholdMinutes));
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        logger.LogInformation(
            "Overdue appointment no-show cleanup started. Interval: {IntervalMinutes} minute(s), grace period: {GraceMinutes} minute(s).",
            intervalMinutes,
            gracePeriod.TotalMinutes);

        using var timer = new PeriodicTimer(interval);

        try
        {
            // Run once at startup, then on every tick.
            await ProcessOverdueAppointmentsAsync(gracePeriod, stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessOverdueAppointmentsAsync(gracePeriod, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down - nothing to do.
        }
    }

    private async Task ProcessOverdueAppointmentsAsync(TimeSpan gracePeriod, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var command = new MarkOverdueAppointmentsAsNoShowCommand(DateTime.UtcNow, gracePeriod);
            var result = await sender.Send(command, stoppingToken);

            if (result.IsSuccess && result.Value > 0)
            {
                logger.LogInformation("Marked {Count} overdue appointment(s) as no-show.", result.Value);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Keep the service alive even if a single run fails.
            logger.LogError(ex, "Failed to mark overdue appointments as no-show.");
        }
    }
}
