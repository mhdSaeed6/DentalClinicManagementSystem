using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Doctors.Queries.GetDoctors;

public class GetDoctorsQueryHandler(IAppDbContext context) : IRequestHandler<GetDoctorsQuery, Result<PaginatedList<DoctorDto>>>
{
    public async Task<Result<PaginatedList<DoctorDto>>> Handle(GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Doctors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.FirstName.ToLower().Contains(search) ||
                d.LastName.ToLower().Contains(search) ||
                d.Specialization.ToLower().Contains(search) ||
                d.ContactInfo.PrimaryPhone.Contains(search) ||
                (d.ContactInfo.SecondaryPhone != null && d.ContactInfo.SecondaryPhone.ToLower().Contains(search)) ||
                (d.ContactInfo.SocialMediaLink != null && d.ContactInfo.SocialMediaLink.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var doctors = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = doctors.ToDtos();

        var paginatedResult = new PaginatedList<DoctorDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return paginatedResult;
    }
}
