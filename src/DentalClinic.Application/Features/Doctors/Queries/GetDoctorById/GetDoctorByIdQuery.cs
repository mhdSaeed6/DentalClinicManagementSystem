using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;

public sealed record GetDoctorByIdQuery(Guid DoctorId) : ICachedQuery<Result<DoctorDto>>
{
    public string CacheKey => $"doctor-{DoctorId}";

    public string[] Tags => ["doctor"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
