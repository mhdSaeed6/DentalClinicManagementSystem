using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Patients.Queries.GetMedicalAlertsCount;

public sealed record GetMedicalAlertsCountQuery(PatientAccessScope Scope)
    : ICachedQuery<Result<int>>
{
    public string CacheKey => $"medical-alerts-count-scope-{Scope.IsRestricted}-{Scope.DoctorId}-{Scope.UserId}";

    public string[] Tags => ["patient"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
