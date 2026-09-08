using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatientById;

public sealed record GetPatientByIdQuery(Guid PatientId) : ICachedQuery<Result<PatientDto>>
{
    public string CacheKey => $"patient-{PatientId}";

    public string[] Tags => ["patient"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}