using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;

public class RemoveTreatmentPlanCommandHandler(
    ILogger<RemoveTreatmentPlanCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<RemoveTreatmentPlanCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Treatment plan removal failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Treatment plan {PlanId} removal denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        // D-P5: demand an explicit confirmation once performed (billed) work
        // exists, so a plan can never disappear silently around an invoice.
        var hasPerformedItems = await context.TreatmentRecords
            .AsNoTracking()
            .AnyAsync(r => r.PlanId == request.PlanId && r.Status == TreatmentRecordStatus.Performed, cancellationToken);
        if (hasPerformedItems && !request.ConfirmPerformedItems)
        {
            logger.LogWarning(
                "Treatment plan {PlanId} removal blocked: performed items exist and no confirmation was supplied.",
                request.PlanId);
            return Domain.TreatmentPlans.TreatmentPlanErrors.PlanHasPerformedItems;
        }

        plan.Delete(user.Id);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        logger.LogInformation("Treatment plan {PlanId} soft-deleted by {UserId}.", request.PlanId, user.Id);

        return Result.Deleted;
    }
}