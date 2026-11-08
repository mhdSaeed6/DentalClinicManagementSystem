using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.UpdateStaffUserRoles;

public sealed class UpdateStaffUserRolesCommandHandler(IIdentityService identityService, IUser user)
    : IRequestHandler<UpdateStaffUserRolesCommand, Result<StaffUserDto>>
{
    public async Task<Result<StaffUserDto>> Handle(UpdateStaffUserRolesCommand request, CancellationToken ct)
    {
        // Two-layer rule: the controller policy is the outer layer; this check
        // is the inner one and must stand even if the policy is ever changed.
        if (!user.IsInRole(Roles.Admin))
        {
            return StaffUserErrors.ManageForbidden;
        }

        // Never allow an admin to demote or disable their own account through
        // the roles edit — it would let the last admin lock themselves out.
        if (user.Id == request.UserId)
        {
            return StaffUserErrors.CannotModifySelf;
        }

        return await identityService.UpdateStaffUserRolesAsync(
            request.UserId,
            request.Roles,
            user.Id,
            ct);
    }
}