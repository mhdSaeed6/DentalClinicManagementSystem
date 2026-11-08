using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Staff;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace DentalClinic.Application.Features.Doctors.Commands.LinkDoctorUser;

public sealed class LinkDoctorUserCommandHandler(IAppDbContext context, IIdentityService identityService, IUser user, HybridCache cache)
    : IRequestHandler<LinkDoctorUserCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(LinkDoctorUserCommand request, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Admin))
        {
            return StaffUserErrors.ManageForbidden;
        }

        var doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, ct);

        if (doctor is null)
        {
            return ApplicationErrors.DoctorNotFound;
        }

        var identityUser = await identityService.GetUserByIdAsync(request.UserId);

        if (identityUser.IsError)
        {
            return ApplicationErrors.UserNotFound;
        }

        if (!identityUser.Value.Roles.Contains(Roles.Doctor))
        {
            return StaffUserErrors.UserNotDoctor;
        }

        // One identity account must drive at most one doctor profile, otherwise
        // the doctor_id claim (and therefore doctor scoping) becomes ambiguous.
        var alreadyLinked = await context.Doctors.AsNoTracking()
            .AnyAsync(d => d.UserId == request.UserId && d.Id != request.DoctorId, ct);

        if (alreadyLinked)
        {
            return StaffUserErrors.DoctorUserAlreadyLinked;
        }

        doctor.AssignUser(request.UserId);

        await context.SaveChangesAsync(ct);

        // The linked user surfaces in cached staff views through this profile.
        await cache.RemoveByTagAsync("doctor", ct);

        return Result.Updated;
    }
}