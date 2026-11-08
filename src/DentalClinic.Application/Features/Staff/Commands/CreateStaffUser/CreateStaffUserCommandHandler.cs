using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<CreateStaffUserCommand, Result<StaffUserDto>>
{
    public async Task<Result<StaffUserDto>> Handle(CreateStaffUserCommand request, CancellationToken ct)
    {
        return await identityService.CreateStaffUserAsync(
            request.Email,
            request.Password,
            request.Roles,
            ct);
    }
}