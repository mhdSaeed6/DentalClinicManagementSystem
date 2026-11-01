using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

public class GetTreatmentPlanByIdQueryHandler(
    ILogger<GetTreatmentPlanByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetTreatmentPlanByIdQuery, Result<TreatmentPlanDetailDto>>
{
    public async Task<Result<TreatmentPlanDetailDto>> Handle(GetTreatmentPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Treatment plan retrieval failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (request.Scope.IsRestricted && plan.DoctorId != request.Scope.DoctorId)
        {
            logger.LogWarning("Treatment plan {PlanId} is outside the caller's authorized scope.", request.PlanId);
            return ApplicationErrors.TreatmentPlanAccessForbidden;
        }

        // Items ordered by session then creation order — the print sheet depends on it.
        var items = await context.TreatmentRecords
            .AsNoTracking()
            .Where(r => r.PlanId == request.PlanId)
            .OrderBy(r => r.SessionNumber)
            .ThenBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var patientName = await context.Patients
            .AsNoTracking()
            .Where(p => p.Id == plan.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var doctorName = await context.Doctors
            .AsNoTracking()
            .Where(d => d.Id == plan.DoctorId)
            .Select(d => d.FirstName + " " + d.LastName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return plan.ToDetailDto(items, patientName, doctorName);
    }
}