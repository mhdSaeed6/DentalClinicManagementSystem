using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

public sealed record GetTreatmentPlanByIdQuery(
    DoctorAccessScope Scope,
    Guid PlanId) : ICachedQuery<Result<TreatmentPlanDetailDto>>
{
    public string CacheKey => $"treatment-plan-{PlanId}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["treatment-plan", "treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}