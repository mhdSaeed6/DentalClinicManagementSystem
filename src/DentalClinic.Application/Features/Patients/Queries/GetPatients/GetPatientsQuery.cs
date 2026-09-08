using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatients;

public sealed record GetPatientsQuery() : ICachedQuery<Result<List<PatientDto>>>
{
    public string CacheKey => "patients";

    public string[] Tags => ["patients"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}