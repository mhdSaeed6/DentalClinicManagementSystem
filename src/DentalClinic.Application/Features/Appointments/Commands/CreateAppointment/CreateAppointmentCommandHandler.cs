using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandHandler(
    ILogger<CreateAppointmentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateAppointmentCommand, Result<AppointmentDto>>
{
    public async Task<Result<AppointmentDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating appointment for patient {PatientId}", request.PatientId);

        // 1. Check Patient Existence & Active State
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
        {
            logger.LogWarning("Appointment creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        // 2. Check Doctor Existence & Active State
        var doctor = await context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            logger.LogWarning("Appointment creation failed. Doctor {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        if (!doctor.IsActive)
        {
            logger.LogWarning("Appointment creation failed. Doctor {DoctorId} is inactive.", request.DoctorId);
            return ApplicationErrors.DoctorInactive;
        }

        // 3. Check Service Existence & Active State
        var service = await context.DentalServices.FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
        if (service is null)
        {
            logger.LogWarning("Appointment creation failed. Service {ServiceId} not found.", request.ServiceId);
            return ApplicationErrors.ServiceNotFound;
        }

        if (!service.IsActive)
        {
            logger.LogWarning("Appointment creation failed. Service {ServiceId} is inactive.", request.ServiceId);
            return ServiceErrors.ServiceInactive;
        }

        // 4. Create Domain Entity
        var appointmentResult = Appointment.Create(
            request.PatientId,
            request.DoctorId,
            request.ServiceId,
            request.ScheduledDateTime,
            request.DurationInMinutes,
            request.Notes);

        if (appointmentResult.IsError)
        {
            logger.LogWarning("Failed to create appointment for patient {PatientId}. Errors: {@Errors}", request.PatientId, appointmentResult.Errors);
            return appointmentResult.Errors;
        }

        var appointment = appointmentResult.Value;

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync(cancellationToken);

        // 5. Invalidate Cache Tag (plural)
        await cache.RemoveByTagAsync("appointments", cancellationToken);

        logger.LogInformation("Appointment created successfully with ID: {AppointmentId}", appointment.Id);

        return appointment.ToDto();
    }
}
