using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.SetStaffUserActive;

public sealed class SetStaffUserActiveCommandHandler(IIdentityService identityService, IUser user)
    : IRequestHandler<SetStaffUserActiveCommand, Result<StaffUserDto>>
{
    public async Task<Result<StaffUserDto>> Handle(SetStaffUserActiveCommand request, CancellationToken ct)
    {
        // Handler-level role check: only administrators may (de)activate staff.
        if (!user.IsInRole(Roles.Admin))
        {
            return StaffUserErrors.ManageForbidden;
        }

        // An admin deactivating themselves would end their own session on the
        // next token refresh — refuse rather than warn.
        if (!request.IsActive && user.Id == request.UserId)
        {
            return StaffUserErrors.CannotModifySelf;
        }

        return await identityService.SetStaffUserActiveAsync(
            request.UserId,
            request.IsActive,
            user.Id,
            ct);
    }
}