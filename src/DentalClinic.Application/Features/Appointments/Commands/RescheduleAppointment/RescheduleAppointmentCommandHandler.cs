using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;

public class RescheduleAppointmentCommandHandler(
    ILogger<RescheduleAppointmentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<RescheduleAppointmentCommand, Result<AppointmentDto>>
{
    public async Task<Result<AppointmentDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments.FindAsync([request.AppointmentId], cancellationToken);
        if (appointment is null)
        {
            logger.LogWarning("Appointment reschedule failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        var result = appointment.Reschedule(request.ScheduledDateTime, request.DurationInMinutes);
        if (result.IsError)
        {
            logger.LogWarning("Failed to reschedule appointment {AppointmentId}. Errors: {@Errors}", request.AppointmentId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"appointment-{request.AppointmentId}", cancellationToken);
        await cache.RemoveByTagAsync("appointment", cancellationToken);

        return appointment.ToDto();
    }
}
