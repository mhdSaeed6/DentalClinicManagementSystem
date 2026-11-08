using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUsers;

public sealed class GetStaffUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetStaffUsersQuery, Result<PaginatedList<StaffUserDto>>>
{
    public async Task<Result<PaginatedList<StaffUserDto>>> Handle(GetStaffUsersQuery request, CancellationToken ct)
    {
        return await identityService.ListStaffUsersAsync(
            request.SearchTerm,
            request.PageNumber,
            request.PageSize,
            ct);
    }
}