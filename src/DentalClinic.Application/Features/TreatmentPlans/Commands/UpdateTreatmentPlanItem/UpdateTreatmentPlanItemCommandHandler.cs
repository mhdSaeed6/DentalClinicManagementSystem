using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlanItem;

// Plan-side edits (estimate, session) apply only while the item is planned —
// the domain enforces it; performed work is corrected through UpdateTreatmentRecord.
public class UpdateTreatmentPlanItemCommandHandler(
    ILogger<UpdateTreatmentPlanItemCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpdateTreatmentPlanItemCommand, Result<TreatmentPlanItemDto>>
{
    public async Task<Result<TreatmentPlanItemDto>> Handle(UpdateTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Plan item update failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Plan {PlanId} item update denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var item = await context.TreatmentRecords
            .FirstOrDefaultAsync(r => r.Id == request.ItemId && r.PlanId == request.PlanId, cancellationToken);
        if (item is null)
        {
            logger.LogWarning("Plan {PlanId} item update failed. Item {ItemId} not found.", request.PlanId, request.ItemId);
            return ApplicationErrors.TreatmentPlanItemNotFound;
        }

        var result = item.UpdatePlanItem(
            request.ToothNumber,
            request.ProcedureDetails,
            request.EstimatedCost,
            request.SessionNumber);

        if (result.IsError)
        {
            logger.LogWarning("Failed to update plan item {ItemId}. Errors: {@Errors}", request.ItemId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return item.ToPlanItemDto();
    }
}