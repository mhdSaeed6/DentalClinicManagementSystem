using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUser;

public sealed class GetStaffUserQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetStaffUserQuery, Result<StaffUserDto>>
{
    public async Task<Result<StaffUserDto>> Handle(GetStaffUserQuery request, CancellationToken ct)
    {
        return await identityService.GetStaffUserAsync(request.UserId, ct);
    }
}