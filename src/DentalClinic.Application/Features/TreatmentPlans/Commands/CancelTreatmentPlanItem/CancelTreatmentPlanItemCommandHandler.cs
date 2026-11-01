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

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;

public class CancelTreatmentPlanItemCommandHandler(
    ILogger<CancelTreatmentPlanItemCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CancelTreatmentPlanItemCommand, Result<TreatmentPlanItemDto>>
{
    public async Task<Result<TreatmentPlanItemDto>> Handle(CancelTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Plan item cancellation failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Plan {PlanId} item cancellation denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var item = await context.TreatmentRecords
            .FirstOrDefaultAsync(r => r.Id == request.ItemId && r.PlanId == request.PlanId, cancellationToken);
        if (item is null)
        {
            logger.LogWarning("Plan {PlanId} item cancellation failed. Item {ItemId} not found.", request.PlanId, request.ItemId);
            return ApplicationErrors.TreatmentPlanItemNotFound;
        }

        var result = item.Cancel();
        if (result.IsError)
        {
            logger.LogWarning("Failed to cancel plan item {ItemId}. Errors: {@Errors}", request.ItemId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        logger.LogInformation("Plan item {ItemId} cancelled in plan {PlanId}.", request.ItemId, request.PlanId);

        return item.ToPlanItemDto();
    }
}