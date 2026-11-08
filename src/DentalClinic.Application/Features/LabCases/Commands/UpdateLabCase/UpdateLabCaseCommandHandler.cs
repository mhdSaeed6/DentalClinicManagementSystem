using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Commands.CreateLabCase;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.LabCases.Commands.UpdateLabCase;

public class UpdateLabCaseCommandHandler(
    ILogger<UpdateLabCaseCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpdateLabCaseCommand, Result<LabCaseDto>>
{
    public async Task<Result<LabCaseDto>> Handle(UpdateLabCaseCommand request, CancellationToken cancellationToken)
    {
        var labCase = await context.LabCases
            .FirstOrDefaultAsync(c => c.Id == request.LabCaseId, cancellationToken);

        if (labCase is null)
        {
            logger.LogWarning("Lab case update failed. Case {LabCaseId} not found.", request.LabCaseId);
            return ApplicationErrors.LabCaseNotFound;
        }

        // The owning doctor is server-derived from the stored row, never from the
        // request: a lab case cannot be reassigned through an update.
        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(labCase.DoctorId))
        {
            logger.LogWarning("Lab case update denied for user {UserId} on case {LabCaseId}.", user.Id, request.LabCaseId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.LabCaseAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        if (request.TreatmentRecordId.HasValue)
        {
            var mismatch = await CreateLabCaseCommandHandler.IsTreatmentRecordMismatchedAsync(
                context,
                request.TreatmentRecordId.Value,
                labCase.PatientId,
                labCase.DoctorId,
                cancellationToken);

            if (mismatch is not null)
            {
                logger.LogWarning(
                    "Lab case update failed. Treatment record {TreatmentRecordId} does not belong to case {LabCaseId}.",
                    request.TreatmentRecordId,
                    request.LabCaseId);
                return mismatch;
            }
        }

        var updateResult = labCase.Update(
            request.LabName,
            request.CaseType,
            request.ExpectedReturnAtUtc,
            request.ToothNumber,
            request.Cost,
            request.Notes,
            request.TreatmentRecordId);

        if (updateResult.IsError)
        {
            logger.LogWarning("Failed to update lab case {LabCaseId}. Errors: {@Errors}", request.LabCaseId, updateResult.Errors);
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("lab-case", cancellationToken);

        var patientName = await context.Patients
            .Where(p => p.Id == labCase.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        var doctorName = await context.Doctors
            .Where(d => d.Id == labCase.DoctorId)
            .Select(d => d.FirstName + " " + d.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        logger.LogInformation("Lab case {LabCaseId} updated.", labCase.Id);

        return labCase.ToDto(DateTimeOffset.UtcNow, patientName, doctorName);
    }
}