using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.MarkNoShowAppointment;

public class MarkNoShowAppointmentCommandHandler(
    ILogger<MarkNoShowAppointmentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<MarkNoShowAppointmentCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(MarkNoShowAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments.FindAsync([request.AppointmentId], cancellationToken);
        if (appointment is null)
        {
            logger.LogWarning("Appointment no-show failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        var result = appointment.MarkAsNoShow();
        if (result.IsError)
        {
            logger.LogWarning("Failed to mark appointment {AppointmentId} as no-show. Errors: {@Errors}", request.AppointmentId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"appointment-{request.AppointmentId}", cancellationToken);
        await cache.RemoveByTagAsync("appointment", cancellationToken);

        return Result.Updated;
    }
}
