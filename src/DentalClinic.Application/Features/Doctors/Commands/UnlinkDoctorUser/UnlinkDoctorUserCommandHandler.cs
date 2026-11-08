using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Staff;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace DentalClinic.Application.Features.Doctors.Commands.UnlinkDoctorUser;

public sealed class UnlinkDoctorUserCommandHandler(IAppDbContext context, IUser user, HybridCache cache)
    : IRequestHandler<UnlinkDoctorUserCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UnlinkDoctorUserCommand request, CancellationToken ct)
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

        doctor.UnassignUser();

        await context.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync("doctor", ct);

        return Result.Updated;
    }
}