using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentPlans;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlan;

public class UpdateTreatmentPlanCommandHandler(
    ILogger<UpdateTreatmentPlanCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpdateTreatmentPlanCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Treatment plan update failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        // Two-layer authorization: clinical-writers only, and a doctor may
        // update only their own plans.
        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Treatment plan {PlanId} update denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var result = plan.Update(request.Title, request.Notes);
        if (result.IsError)
        {
            logger.LogWarning("Failed to update treatment plan {PlanId}. Errors: {@Errors}", request.PlanId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return result.Value;
    }
}