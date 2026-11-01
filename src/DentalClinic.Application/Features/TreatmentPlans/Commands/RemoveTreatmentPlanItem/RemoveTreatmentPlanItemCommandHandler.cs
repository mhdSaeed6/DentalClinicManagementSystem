using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlanItem;

public class RemoveTreatmentPlanItemCommandHandler(
    ILogger<RemoveTreatmentPlanItemCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<RemoveTreatmentPlanItemCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Plan item removal failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Plan {PlanId} item removal denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var item = await context.TreatmentRecords
            .FirstOrDefaultAsync(r => r.Id == request.ItemId && r.PlanId == request.PlanId, cancellationToken);
        if (item is null)
        {
            logger.LogWarning("Plan {PlanId} item removal failed. Item {ItemId} not found.", request.PlanId, request.ItemId);
            return ApplicationErrors.TreatmentPlanItemNotFound;
        }

        item.Delete(user.Id);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        logger.LogInformation("Plan item {ItemId} soft-removed from plan {PlanId}.", request.ItemId, request.PlanId);

        return Result.Deleted;
    }
}