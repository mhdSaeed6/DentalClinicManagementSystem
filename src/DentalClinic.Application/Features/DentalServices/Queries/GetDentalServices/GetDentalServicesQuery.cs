using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;

public sealed record GetDentalServicesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) : ICachedQuery<Result<PaginatedList<DentalServiceDto>>>
{
    public string CacheKey => $"services-page-{PageNumber}-size-{PageSize}-search-{SearchTerm ?? "all"}";

    public string[] Tags => ["service"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
