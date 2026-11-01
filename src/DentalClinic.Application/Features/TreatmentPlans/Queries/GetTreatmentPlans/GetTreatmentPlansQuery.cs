using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentPlans;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;

// Scope is server-derived (the controller passes user.DoctorScope()) but must
// stay part of the cache key: a doctor's plans must never be served from a
// cache entry a receptionist populated.
public sealed record GetTreatmentPlansQuery(
    DoctorAccessScope Scope,
    int PageNumber = 1,
    int PageSize = 10,
    Guid? PatientId = null,
    Guid? DoctorId = null,
    TreatmentPlanStatus? Status = null) : ICachedQuery<Result<PaginatedList<TreatmentPlanDto>>>
{
    public string CacheKey => $"treatment-plans-page-{PageNumber}-size-{PageSize}-patient-{PatientId?.ToString() ?? "all"}-doctor-{DoctorId?.ToString() ?? "all"}-status-{Status?.ToString() ?? "all"}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    // Both tags: plan totals derive from their items (treatment records).
    public string[] Tags => ["treatment-plan", "treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}