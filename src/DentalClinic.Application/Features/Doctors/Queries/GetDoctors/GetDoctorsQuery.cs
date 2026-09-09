using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Doctors.Queries.GetDoctors;

public sealed record GetDoctorsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) : ICachedQuery<Result<PaginatedList<DoctorDto>>>
{
    public string CacheKey => $"doctors-page-{PageNumber}-size-{PageSize}-search-{SearchTerm ?? "all"}";

    public string[] Tags => ["doctor"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
