using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
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
    HybridCache cache)
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