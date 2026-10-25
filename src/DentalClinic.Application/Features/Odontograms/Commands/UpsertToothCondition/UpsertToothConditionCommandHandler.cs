using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Exceptions;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Application.Features.Odontograms.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Odontograms;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;

public class UpsertToothConditionCommandHandler(
    ILogger<UpsertToothConditionCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpsertToothConditionCommand, Result<PatientToothSummaryDto>>
{
    public async Task<Result<PatientToothSummaryDto>> Handle(UpsertToothConditionCommand request, CancellationToken cancellationToken)
    {
        // 0. Authorization: only admins and doctors set clinical tooth state, and
        //    the patient gate follows the treatment-record precedence — the write
        //    capability decides between ClinicalWriteForbidden and (further down)
        //    the patient-scope PatientAccessForbidden. The client is never trusted
        //    with an identity: Scope comes server-derived from IUser.PatientScope().
        if (!user.CanWriteClinicalRecords())
        {
            logger.LogWarning(
                "Tooth condition upsert denied for user {UserId} on patient {PatientId} (tooth {ToothNumber}).",
                user.Id, request.PatientId, request.ToothNumber);
            return ApplicationErrors.ClinicalWriteForbidden;
        }

        var patient = await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning(
                "Tooth condition upsert failed. Patient {PatientId} not found (tooth {ToothNumber}).",
                request.PatientId, request.ToothNumber);
            return ApplicationErrors.PatientNotFound;
        }

        if (!await PatientAccess.CanAccessAsync(context, request.Scope, patient, cancellationToken))
        {
            logger.LogWarning(
                "Tooth condition upsert denied. Patient {PatientId} is outside the caller's authorized scope (tooth {ToothNumber}).",
                request.PatientId, request.ToothNumber);
            return ApplicationErrors.PatientAccessForbidden;
        }

        // The soft-delete query filter keeps soft-deleted rows out of this lookup,
        // so the upsert targets the live row only; a new row after a clear is
        // permitted by the filtered unique index.
        var tooth = await context.PatientTeeth
            .FirstOrDefaultAsync(t => t.PatientId == request.PatientId && t.ToothNumber == request.ToothNumber, cancellationToken);

        if (tooth is not null)
        {
            var updateResult = tooth.Update(request.Condition, request.Surface, request.Notes);
            if (updateResult.IsError)
            {
                logger.LogWarning(
                    "Failed to update the condition of tooth {ToothNumber} for patient {PatientId}. Errors: {@Errors}",
                    request.ToothNumber, request.PatientId, updateResult.Errors);
                return updateResult.Errors;
            }
        }
        else
        {
            var createResult = PatientTooth.Create(
                request.PatientId, request.ToothNumber, request.Condition, request.Surface, request.Notes);

            if (createResult.IsError)
            {
                logger.LogWarning(
                    "Failed to create the condition of tooth {ToothNumber} for patient {PatientId}. Errors: {@Errors}",
                    request.ToothNumber, request.PatientId, createResult.Errors);
                return createResult.Errors;
            }

            tooth = createResult.Value;
            context.PatientTeeth.Add(tooth);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueIndexViolationException)
        {
            // Infrastructure translated the provider-specific unique-index
            // violation (SQL Server 2601/2605) into this provider-neutral type:
            // a concurrent upsert of the same (patient, tooth) created the row
            // first, so the loser reports 409 instead of a 500 (plan decision 8:
            // concurrency by index, not transaction). Any other failure
            // propagates unchanged.
            logger.LogWarning(
                "Concurrent update on tooth {ToothNumber} for patient {PatientId}: the unique index rejected the second insert.",
                request.ToothNumber, request.PatientId);
            return ApplicationErrors.ToothConditionConflict;
        }

        await cache.RemoveByTagAsync("patient-tooth", cancellationToken);

        return tooth.ToSummaryDto();
    }
}