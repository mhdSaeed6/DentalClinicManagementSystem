using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.ResetStaffUserPassword;

public sealed class ResetStaffUserPasswordCommandHandler(IIdentityService identityService, IUser user)
    : IRequestHandler<ResetStaffUserPasswordCommand, Result<StaffUserDto>>
{
    public async Task<Result<StaffUserDto>> Handle(ResetStaffUserPasswordCommand request, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Admin))
        {
            return StaffUserErrors.ManageForbidden;
        }

        return await identityService.ResetStaffUserPasswordAsync(
            request.UserId,
            request.NewPassword,
            ct);
    }
}