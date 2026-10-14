using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;
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

        // Optional service replacement, validated exactly like the create flow.
        // Only this appointment's ServiceId reference can change; the service
        // definition (name/price) and other appointments are never touched.
        if (request.ServiceId is { } serviceId)
        {
            var service = await context.DentalServices.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
            if (service is null)
            {
                logger.LogWarning("Appointment complete failed. Service {ServiceId} not found.", serviceId);
                return ApplicationErrors.ServiceNotFound;
            }

            if (!service.IsActive)
            {
                logger.LogWarning("Appointment complete failed. Service {ServiceId} is inactive.", serviceId);
                return ServiceErrors.ServiceInactive;
            }
        }

        // Complete first. This also guards against retries: an appointment that is
        // already completed/cancelled/no-show returns an error and no treatment is
        // ever created, so retrying the action cannot duplicate treatments.
        // An omitted ServiceId keeps the appointment's current service unchanged.
        var result = request.ServiceId.HasValue
            ? appointment.Complete(request.ServiceId)
            : appointment.Complete();
        if (result.IsError)
        {
            logger.LogWarning("Failed to complete appointment {AppointmentId}. Errors: {@Errors}", request.AppointmentId, result.Errors);
            return result.Errors;
        }

        IReadOnlyList<CompleteAppointmentTreatment> treatments = request.Treatments ?? [];
        var created = new List<TreatmentRecord>(treatments.Count);

        foreach (var treatment in treatments)
        {
            // Patient, doctor and appointment are taken from the appointment being
            // completed, never from the caller, so a treatment can never be attached
            // to a different patient's visit by mistake.
            var createResult = TreatmentRecord.Create(
                appointment.PatientId,
                appointment.DoctorId,
                treatment.ToothNumber,
                treatment.ProcedureDetails,
                treatment.Cost,
                appointment.Id);

            if (createResult.IsError)
            {
                logger.LogWarning(
                    "Failed to record treatment for appointment {AppointmentId}. Errors: {@Errors}",
                    request.AppointmentId,
                    createResult.Errors);
                return createResult.Errors;
            }

            created.Add(createResult.Value);
        }

        if (created.Count > 0)
        {
            context.TreatmentRecords.AddRange(created);
        }

        // Persisting the status change and every treatment in a single SaveChanges
        // keeps the visit atomic: a failure cannot leave a partially recorded visit.
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"appointment-{request.AppointmentId}", cancellationToken);
        await cache.RemoveByTagAsync("appointment", cancellationToken);

        if (created.Count > 0)
        {
            await cache.RemoveByTagAsync("treatment-record", cancellationToken);
        }

        return Result.Updated;
    }
}
