using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;

public class GetTreatmentPlansQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTreatmentPlansQuery, Result<PaginatedList<TreatmentPlanDto>>>
{
    public async Task<Result<PaginatedList<TreatmentPlanDto>>> Handle(GetTreatmentPlansQuery request, CancellationToken cancellationToken)
    {
        var query = context.TreatmentPlans.AsNoTracking();

        // Trusted, server-derived scope: a doctor may only read their own plans.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            query = query.Where(p => p.DoctorId == scopeDoctorId);
        }

        if (request.PatientId.HasValue)
        {
            query = query.Where(p => p.PatientId == request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            query = query.Where(p => p.DoctorId == request.DoctorId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var plans = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var planIds = plans.Select(p => p.Id).ToList();

        // Totals over the plan's items — one grouped round trip, all statuses
        // (planned counts its estimate, performed its charge, cancelled neither).
        var totals = await context.TreatmentRecords
            .AsNoTracking()
            .Where(r => r.PlanId != null && planIds.Contains(r.PlanId.Value))
            .GroupBy(r => r.PlanId)
            .Select(g => new
            {
                PlanId = g.Key!.Value,
                ItemCount = g.Count(),
                EstimatedTotal = g
                    .Sum(r => r.Status == Domain.TreatmentRecords.TreatmentRecordStatus.Planned
                        ? (r.EstimatedCost ?? 0m)
                        : 0m),
                PerformedTotal = g
                    .Sum(r => r.Status == Domain.TreatmentRecords.TreatmentRecordStatus.Performed ? r.Cost : 0m)
            })
            .ToListAsync(cancellationToken);

        var patientNames = await context.Patients
            .AsNoTracking()
            .Where(p => plans.Select(x => x.PatientId).Distinct().Contains(p.Id))
            .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
            .ToListAsync(cancellationToken);

        var doctorNames = await context.Doctors
            .AsNoTracking()
            .Where(d => plans.Select(x => x.DoctorId).Distinct().Contains(d.Id))
            .Select(d => new { d.Id, Name = d.FirstName + " " + d.LastName })
            .ToListAsync(cancellationToken);

        var totalsByPlan = totals.ToDictionary(t => t.PlanId);
        var patientsById = patientNames.ToDictionary(p => p.Id, p => p.Name);
        var doctorsById = doctorNames.ToDictionary(d => d.Id, d => d.Name);

        var items = plans.Select(p =>
        {
            var t = totalsByPlan.TryGetValue(p.Id, out var value)
                ? new TreatmentPlanTotals(value.ItemCount, value.EstimatedTotal, value.PerformedTotal)
                : default(TreatmentPlanTotals);
            return p.ToDto(t, patientsById.GetValueOrDefault(p.PatientId, string.Empty), doctorsById.GetValueOrDefault(p.DoctorId, string.Empty));
        }).ToList();

        return new PaginatedList<TreatmentPlanDto>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}