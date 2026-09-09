using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;

public sealed record GetDentalServiceByIdQuery(Guid DentalServiceId) : ICachedQuery<Result<DentalServiceDto>>
{
    public string CacheKey => $"service-{DentalServiceId}";

    public string[] Tags => ["service"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
