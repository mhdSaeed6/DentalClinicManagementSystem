using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;

public class GetTreatmentPlanSessionsQueryHandler(
    ILogger<GetTreatmentPlanSessionsQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetTreatmentPlanSessionsQuery, Result<List<TreatmentPlanSessionDto>>>
{
    public async Task<Result<List<TreatmentPlanSessionDto>>> Handle(GetTreatmentPlanSessionsQuery request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Treatment plan sessions retrieval failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (request.Scope.IsRestricted && plan.DoctorId != request.Scope.DoctorId)
        {
            logger.LogWarning("Treatment plan {PlanId} sessions are outside the caller's authorized scope.", request.PlanId);
            return ApplicationErrors.TreatmentPlanAccessForbidden;
        }

        var items = await context.TreatmentRecords
            .AsNoTracking()
            .Where(r => r.PlanId == request.PlanId)
            .Select(r => new { r.SessionNumber, Status = r.Status, Amount = r.Status == TreatmentRecordStatus.Planned ? (r.EstimatedCost ?? 0m) : r.Cost })
            .ToListAsync(cancellationToken);

        // One row per session. ItemCount includes cancelled items (history),
        // while money totals contribute cancelled items nothing.
        List<TreatmentPlanSessionDto> sessions = [.. items
            .GroupBy(i => i.SessionNumber)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var estimated = g.Where(x => x.Status == TreatmentRecordStatus.Planned).Sum(x => x.Amount);
                var performed = g.Where(x => x.Status == TreatmentRecordStatus.Performed).Sum(x => x.Amount);

                return new TreatmentPlanSessionDto(
                    g.Key,
                    g.Count(),
                    estimated,
                    performed,
                    estimated + performed);
            })];

        return sessions;
    }
}