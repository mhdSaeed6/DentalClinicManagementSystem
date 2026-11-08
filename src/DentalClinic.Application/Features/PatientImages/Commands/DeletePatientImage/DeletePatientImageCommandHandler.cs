using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.PatientImages.Commands.DeletePatientImage;

public class DeletePatientImageCommandHandler(
    ILogger<DeletePatientImageCommandHandler> logger,
    IAppDbContext context,
    IImageStorage storage,
    HybridCache cache,
    IUser user)
    : IRequestHandler<DeletePatientImageCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeletePatientImageCommand request, CancellationToken cancellationToken)
    {
        if (!user.CanWriteClinicalRecords())
        {
            logger.LogWarning("Image deletion denied for user {UserId} (image {ImageId}).", user.Id, request.ImageId);
            return ApplicationErrors.ClinicalWriteForbidden;
        }

        var image = await context.PatientImages
            .FirstOrDefaultAsync(
                i => i.Id == request.ImageId && i.PatientId == request.PatientId,
                cancellationToken);

        if (image is null)
        {
            return ApplicationErrors.PatientImageNotFound;
        }

        // Soft delete first so the row can never point at a file that is about
        // to be removed; the binary is then deleted best-effort.
        image.Delete(user.Id);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("patient-image", cancellationToken);

        await storage.DeleteAsync(image.StoragePath, cancellationToken);

        logger.LogInformation(
            "Patient image {ImageId} deleted from patient {PatientId} by {UserId}.",
            image.Id,
            image.PatientId,
            user.Id);

        return Result.Deleted;
    }
}