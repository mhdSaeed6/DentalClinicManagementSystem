using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;

// Cost grouped by SessionNumber — the per-session rows of the print sheet.
public sealed record GetTreatmentPlanSessionsQuery(
    DoctorAccessScope Scope,
    Guid PlanId) : ICachedQuery<Result<List<TreatmentPlanSessionDto>>>
{
    public string CacheKey => $"treatment-plan-{PlanId}-sessions-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["treatment-plan", "treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}