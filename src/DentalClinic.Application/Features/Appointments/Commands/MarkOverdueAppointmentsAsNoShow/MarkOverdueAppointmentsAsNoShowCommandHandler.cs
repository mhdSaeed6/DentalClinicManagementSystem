using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.MarkOverdueAppointmentsAsNoShow;

public class MarkOverdueAppointmentsAsNoShowCommandHandler(
    ILogger<MarkOverdueAppointmentsAsNoShowCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<MarkOverdueAppointmentsAsNoShowCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MarkOverdueAppointmentsAsNoShowCommand request, CancellationToken cancellationToken)
    {
        var cutoff = request.AsOfUtc - request.GracePeriod;

        // Narrow the set with a translatable filter on the start time only, then apply
        // the duration-aware check in memory so the query stays provider agnostic.
        var candidates = await context.Appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled && a.ScheduledDateTime <= cutoff)
            .ToListAsync(cancellationToken);

        var overdue = candidates.Where(a => a.HasEndedBy(cutoff)).ToList();
        if (overdue.Count == 0)
        {
            return 0;
        }

        foreach (var appointment in overdue)
        {
            appointment.MarkAsNoShow();
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("appointment", cancellationToken);

        logger.LogInformation("Marked {Count} overdue appointment(s) as no-show.", overdue.Count);

        return overdue.Count;
    }
}
