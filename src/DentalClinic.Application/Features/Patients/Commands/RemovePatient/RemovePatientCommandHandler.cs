using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Patients.Commands.RemovePatient;

public class RemovePatientCommandHandler(
    ILogger<RemovePatientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser currentUser)
    : IRequestHandler<RemovePatientCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemovePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await context.Patients.FindAsync([request.PatientId], cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Patient deletion failed. Patient with ID {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        var hasAppointments = await context.Appointments
            .AnyAsync(a => a.PatientId == request.PatientId, cancellationToken);

        if (hasAppointments)
        {
            logger.LogWarning("Cannot delete patient with ID {PatientId} because they have associated appointments.", request.PatientId);
            return ApplicationErrors.PatientHasAppointments;
        }

        // تمرير ID المستخدم الحدوثي المأخوذ من IUser لتدوين من قام بالمسح
        patient.Delete(currentUser.Id);

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"patient-{request.PatientId}", cancellationToken);
        await cache.RemoveByTagAsync("patients", cancellationToken);

        logger.LogInformation("Patient {PatientId} soft-deleted by User {UserId}.", request.PatientId, currentUser.Id);

        return Result.Deleted;
    }
}