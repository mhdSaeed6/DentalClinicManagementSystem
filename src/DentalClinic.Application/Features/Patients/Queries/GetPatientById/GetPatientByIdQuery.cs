using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatientById;

public sealed record GetPatientByIdQuery(Guid PatientId, PatientAccessScope Scope) : ICachedQuery<Result<PatientDto>>
{
    public string CacheKey => $"patient-{PatientId}-scope-{Scope.IsRestricted}-{Scope.DoctorId?.ToString() ?? "all"}-{Scope.UserId ?? "all"}";

    public string[] Tags => ["patient"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
