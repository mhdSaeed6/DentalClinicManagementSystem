using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentPlans;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CompleteTreatmentPlan;

public class CompleteTreatmentPlanCommandHandler(
    ILogger<CompleteTreatmentPlanCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CompleteTreatmentPlanCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(CompleteTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Treatment plan completion failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Treatment plan {PlanId} completion denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var result = plan.Complete();
        if (result.IsError)
        {
            logger.LogWarning("Failed to complete treatment plan {PlanId}. Errors: {@Errors}", request.PlanId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return result.Value;
    }
}