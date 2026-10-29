using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;

public class RemoveToothConditionCommandHandler(
    ILogger<RemoveToothConditionCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<RemoveToothConditionCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveToothConditionCommand request, CancellationToken cancellationToken)
    {
        // 0. Authorization: only admins and doctors clear clinical tooth state, and
        //    the patient gate follows the treatment-record precedence — the write
        //    capability decides between ClinicalWriteForbidden and (further down)
        //    the patient-scope PatientAccessForbidden. The client is never trusted
        //    with an identity: Scope comes server-derived from IUser.PatientScope().
        if (!user.CanWriteClinicalRecords())
        {
            logger.LogWarning(
                "Tooth condition removal denied for user {UserId} on patient {PatientId} (tooth {ToothNumber}).",
                user.Id, request.PatientId, request.ToothNumber);
            return ApplicationErrors.ClinicalWriteForbidden;
        }

        var patient = await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning(
                "Tooth condition removal failed. Patient {PatientId} not found (tooth {ToothNumber}).",
                request.PatientId, request.ToothNumber);
            return ApplicationErrors.PatientNotFound;
        }

        if (!await PatientAccess.CanAccessAsync(context, request.Scope, patient, cancellationToken))
        {
            logger.LogWarning(
                "Tooth condition removal denied. Patient {PatientId} is outside the caller's authorized scope (tooth {ToothNumber}).",
                request.PatientId, request.ToothNumber);
            return ApplicationErrors.PatientAccessForbidden;
        }

        // The soft-delete query filter keeps soft-deleted rows out of this
        // lookup, so an absent row (never set, or already cleared) is an
        // idempotent success — no second delete, no error.
        var tooth = await context.PatientTeeth
            .FirstOrDefaultAsync(t => t.PatientId == request.PatientId && t.ToothNumber == request.ToothNumber, cancellationToken);

        if (tooth is not null)
        {
            tooth.Delete(user.Id);
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation(
                "Tooth condition removal is a no-op; no live row for tooth {ToothNumber} on patient {PatientId}.",
                request.ToothNumber, request.PatientId);
        }

        await cache.RemoveByTagAsync("patient-tooth", cancellationToken);

        return Result.Deleted;
    }
}