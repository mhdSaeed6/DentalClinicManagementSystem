using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Doctors;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;

public class UpdateDoctorCommandHandler(
    ILogger<UpdateDoctorCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache) :
    IRequestHandler<UpdateDoctorCommand, Result<DoctorDto>>
{
    public async Task<Result<DoctorDto>> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating doctor with ID: {DoctorId}", request.DoctorId);

        var doctor = await context.Doctors.FindAsync([request.DoctorId], cancellationToken);

        if (doctor is null)
        {
            logger.LogWarning("Doctor update failed. Doctor with ID {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        var updateResult = doctor.Update(
            request.FirstName,
            request.LastName,
            request.Specialization,
            request.ContactInfo,
            request.Gender);

        if (updateResult.IsError)
        {
            logger.LogWarning(
                "Failed to update doctor with ID {DoctorId}. Errors: {@Errors}",
                request.DoctorId,
                updateResult.Errors);

            return updateResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"doctor-{doctor.Id}", cancellationToken);
        await cache.RemoveByTagAsync("doctor", cancellationToken);

        logger.LogInformation("Doctor updated successfully with ID: {DoctorId}", doctor.Id);

        return doctor.ToDto();
    }
}
