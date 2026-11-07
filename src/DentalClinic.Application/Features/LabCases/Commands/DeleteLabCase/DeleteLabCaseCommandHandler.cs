using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.LabCases.Commands.DeleteLabCase;

public class DeleteLabCaseCommandHandler(
    ILogger<DeleteLabCaseCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<DeleteLabCaseCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteLabCaseCommand request, CancellationToken cancellationToken)
    {
        var labCase = await context.LabCases
            .FirstOrDefaultAsync(c => c.Id == request.LabCaseId, cancellationToken);

        if (labCase is null)
        {
            logger.LogWarning("Lab case deletion failed. Case {LabCaseId} not found.", request.LabCaseId);
            return ApplicationErrors.LabCaseNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(labCase.DoctorId))
        {
            logger.LogWarning("Lab case deletion denied for user {UserId} on case {LabCaseId}.", user.Id, request.LabCaseId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.LabCaseAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        // Soft delete: the row and its audit trail stay in place.
        labCase.Delete(user.Id);

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("lab-case", cancellationToken);

        logger.LogInformation("Lab case {LabCaseId} soft-deleted by user {UserId}.", labCase.Id, user.Id);

        return Result.Deleted;
    }
}