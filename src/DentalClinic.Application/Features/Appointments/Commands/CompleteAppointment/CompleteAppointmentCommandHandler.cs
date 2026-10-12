using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

public class CompleteAppointmentCommandHandler(
    ILogger<CompleteAppointmentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CompleteAppointmentCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments.FindAsync([request.AppointmentId], cancellationToken);
        if (appointment is null)
        {
            logger.LogWarning("Appointment complete failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        if (!user.CanAccessAppointment(appointment.DoctorId))
        {
            logger.LogWarning("Appointment complete denied for user {UserId} on appointment {AppointmentId}.", user.Id, request.AppointmentId);
            return ApplicationErrors.AppointmentAccessForbidden;
        }

        var result = appointment.Complete();
        if (result.IsError)
        {
            logger.LogWarning("Failed to complete appointment {AppointmentId}. Errors: {@Errors}", request.AppointmentId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"appointment-{request.AppointmentId}", cancellationToken);
        await cache.RemoveByTagAsync("appointment", cancellationToken);

        return Result.Updated;
    }
}
