using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;

public class CreateTreatmentRecordCommandHandler(
    ILogger<CreateTreatmentRecordCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateTreatmentRecordCommand, Result<TreatmentRecordDto>>
{
    public async Task<Result<TreatmentRecordDto>> Handle(CreateTreatmentRecordCommand request, CancellationToken cancellationToken)
    {
        var patientExists = await context.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            logger.LogWarning("Treatment record creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        var doctorExists = await context.Doctors.AnyAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (!doctorExists)
        {
            logger.LogWarning("Treatment record creation failed. Doctor {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        if (request.AppointmentId.HasValue)
        {
            var appointmentExists = await context.Appointments.AnyAsync(a => a.Id == request.AppointmentId.Value, cancellationToken);
            if (!appointmentExists)
            {
                logger.LogWarning("Treatment record creation failed. Appointment {AppointmentId} not found.", request.AppointmentId);
                return ApplicationErrors.AppointmentNotFound;
            }
        }

        var result = TreatmentRecord.Create(
            request.PatientId,
            request.DoctorId,
            request.ToothNumber,
            request.ProcedureDetails,
            request.Cost,
            request.AppointmentId);

        if (result.IsError)
        {
            logger.LogWarning("Failed to create treatment record. Errors: {@Errors}", result.Errors);
            return result.Errors;
        }

        var treatmentRecord = result.Value;

        context.TreatmentRecords.Add(treatmentRecord);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return treatmentRecord.ToDto();
    }
}
