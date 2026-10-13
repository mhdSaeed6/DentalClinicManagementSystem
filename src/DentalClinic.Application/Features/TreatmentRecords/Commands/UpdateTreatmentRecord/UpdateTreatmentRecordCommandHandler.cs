using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Mappers;
using DentalClinic.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;

public class UpdateTreatmentRecordCommandHandler(
    ILogger<UpdateTreatmentRecordCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpdateTreatmentRecordCommand, Result<TreatmentRecordDto>>
{
    public async Task<Result<TreatmentRecordDto>> Handle(UpdateTreatmentRecordCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating treatment record with ID: {TreatmentRecordId}", request.TreatmentRecordId);

        var treatmentRecord = await context.TreatmentRecords
            .FirstOrDefaultAsync(t => t.Id == request.TreatmentRecordId, cancellationToken);

        if (treatmentRecord is null)
        {
            logger.LogWarning("Update failed. Treatment record with ID {TreatmentRecordId} not found.", request.TreatmentRecordId);
            return ApplicationErrors.TreatmentRecordNotFound;
        }

        // Authorization: only admins and doctors may modify clinical records; a
        // doctor may only modify their own records and cannot reassign them.
        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(treatmentRecord.DoctorId) || !user.CanAccessAppointment(request.DoctorId))
        {
            logger.LogWarning("Treatment record update denied for user {UserId} on record {TreatmentRecordId}.", user.Id, request.TreatmentRecordId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentRecordAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var updateResult = treatmentRecord.Update(
            request.PatientId,
            request.DoctorId,
            request.ToothNumber,
            request.ProcedureDetails.Trim(),
            request.Cost,
            request.AppointmentId);

        if (updateResult.IsError)
        {
            logger.LogWarning("Failed to update treatment record {TreatmentRecordId}. Errors: {@Errors}", request.TreatmentRecordId, updateResult.Errors);
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"treatment-record-{treatmentRecord.Id}", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        logger.LogInformation("Treatment record updated successfully with ID: {TreatmentRecordId}", treatmentRecord.Id);

        return treatmentRecord.ToDto();
    }
}