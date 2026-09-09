using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;

public class RemoveDoctorCommandHandler(
    ILogger<RemoveDoctorCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<RemoveDoctorCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await context.Doctors.FindAsync([request.DoctorId], cancellationToken);

        if (doctor is null)
        {
            logger.LogWarning("Doctor removal failed. Doctor with ID {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        doctor.Deactivate();

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"doctor-{request.DoctorId}", cancellationToken);
        await cache.RemoveByTagAsync("doctor", cancellationToken);

        logger.LogInformation("Doctor deactivated successfully with ID: {DoctorId}", doctor.Id);

        return Result.Deleted;
    }
}
