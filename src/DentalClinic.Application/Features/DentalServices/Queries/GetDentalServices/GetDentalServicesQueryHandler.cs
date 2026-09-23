using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;

public class GetDentalServicesQueryHandler(IAppDbContext context) : IRequestHandler<GetDentalServicesQuery, Result<PaginatedList<DentalServiceDto>>>
{
    public async Task<Result<PaginatedList<DentalServiceDto>>> Handle(GetDentalServicesQuery request, CancellationToken cancellationToken)
    {
        var query = context.DentalServices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(service =>
                        service.Name.ToLower().Contains(search.ToLower()) ||
                (service.Description != null && service.Description.ToLower().Contains(search.ToLower())));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var services = await query
            .OrderByDescending(service => service.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<DentalServiceDto>
        {
            Items = services.ToDtos(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
