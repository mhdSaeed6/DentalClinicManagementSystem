using System.Data;

using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;

public class RescheduleAppointmentCommandHandler(
    ILogger<RescheduleAppointmentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<RescheduleAppointmentCommand, Result<AppointmentDto>>
{
    public async Task<Result<AppointmentDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        // The overlap check below is read-then-write. Two concurrent requests
        // for the same slot would both read "no conflict" and both write, so the
        // whole check-and-update runs inside a serializable transaction — the
        // same pattern the create path uses.
        await using var transaction = await context.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var appointment = await context.Appointments.FindAsync([request.AppointmentId], cancellationToken);
        if (appointment is null)
        {
            logger.LogWarning("Appointment reschedule failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        if (!user.CanAccessAppointment(appointment.DoctorId))
        {
            logger.LogWarning("Appointment reschedule denied for user {UserId} on appointment {AppointmentId}.", user.Id, request.AppointmentId);
            return ApplicationErrors.AppointmentAccessForbidden;
        }

        // Prevent double-booking: the doctor must not have another scheduled
        // appointment whose time range overlaps the requested one.
        var requestedEnd = request.ScheduledDateTime.AddMinutes(request.DurationInMinutes);
        var hasConflict = await context.Appointments.AnyAsync(
            a => a.DoctorId == appointment.DoctorId &&
                 a.Id != request.AppointmentId &&
                 a.Status == AppointmentStatus.Scheduled &&
                 a.ScheduledDateTime < requestedEnd &&
                 request.ScheduledDateTime < a.ScheduledDateTime.AddMinutes(a.DurationInMinutes),
            cancellationToken);

        if (hasConflict)
        {
            logger.LogWarning(
                "Appointment reschedule failed. Doctor {DoctorId} already has an appointment overlapping {ScheduledDateTime}.",
                appointment.DoctorId,
                request.ScheduledDateTime);
            return ApplicationErrors.AppointmentSlotConflict;
        }

        var result = appointment.Reschedule(request.ScheduledDateTime, request.DurationInMinutes);
        if (result.IsError)
        {
            logger.LogWarning("Failed to reschedule appointment {AppointmentId}. Errors: {@Errors}", request.AppointmentId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RemoveAsync($"appointment-{request.AppointmentId}", cancellationToken);
        await cache.RemoveByTagAsync("appointment", cancellationToken);

        return appointment.ToDto();
    }
}
