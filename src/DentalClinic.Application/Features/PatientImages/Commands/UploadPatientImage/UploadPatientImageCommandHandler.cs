using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Files;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Application.Features.PatientImages.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.PatientImages.Commands.UploadPatientImage;

public class UploadPatientImageCommandHandler(
    ILogger<UploadPatientImageCommandHandler> logger,
    IAppDbContext context,
    IImageStorage storage,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UploadPatientImageCommand, Result<PatientImageDto>>
{
    public async Task<Result<PatientImageDto>> Handle(UploadPatientImageCommand request, CancellationToken cancellationToken)
    {
        // 0. Authorization: images are clinical data, so only admins and doctors
        //    may upload them.
        if (!user.CanWriteClinicalRecords())
        {
            logger.LogWarning("Image upload denied for user {UserId} (patient {PatientId}).", user.Id, request.PatientId);
            return ApplicationErrors.ClinicalWriteForbidden;
        }

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return ApplicationErrors.UserIdClaimInvalid;
        }

        var patientExists = await context.Patients
            .AnyAsync(p => p.Id == request.PatientId, cancellationToken);

        if (!patientExists)
        {
            return ApplicationErrors.PatientNotFound;
        }

        if (request.Length <= 0)
        {
            return PatientImageErrors.InvalidSize;
        }

        if (request.Length > ImageUploadRules.MaxSizeBytes)
        {
            return PatientImageErrors.FileTooLarge;
        }

        // 1. Buffer with the cap enforced while copying: the client-supplied
        //    length is only a hint, never the authority.
        using var buffer = new MemoryStream();

        try
        {
            await request.Content.CopyToAsync(buffer, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Reading the uploaded image failed for patient {PatientId}.", request.PatientId);
            return PatientImageErrors.UnsupportedFormat;
        }

        if (buffer.Length > ImageUploadRules.MaxSizeBytes)
        {
            return PatientImageErrors.FileTooLarge;
        }

        // 2. Decide the real format from the magic bytes.
        var header = new byte[12];
        buffer.Position = 0;
        var headerLength = await buffer.ReadAsync(header, cancellationToken);

        if (!ImageUploadRules.TryDetect(header.AsSpan(0, headerLength), out var detectedContentType, out var extension))
        {
            return PatientImageErrors.UnsupportedFormat;
        }

        var declaredContentType = ImageUploadRules.NormalizeContentType(request.ContentType);

        if (declaredContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            return PatientImageErrors.UnsupportedContentType;
        }

        if (!string.Equals(declaredContentType, detectedContentType, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Rejected image upload for patient {PatientId}: declared {Declared} but content is {Detected}.",
                request.PatientId,
                declaredContentType,
                detectedContentType);
            return PatientImageErrors.UnsupportedFormat;
        }

        // 3. Tooth-link integrity: a linked treatment record must belong to the
        //    same patient, and an explicit tooth must agree with the record.
        if (request.TreatmentRecordId.HasValue)
        {
            var treatment = await context.TreatmentRecords
                .AsNoTracking()
                .Where(t => t.Id == request.TreatmentRecordId.Value)
                .Select(t => new { t.PatientId, t.ToothNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (treatment is null || treatment.PatientId != request.PatientId)
            {
                return ApplicationErrors.PatientImageTreatmentRecordMismatch;
            }

            if (treatment.ToothNumber.HasValue &&
                request.ToothNumber.HasValue &&
                treatment.ToothNumber.Value != request.ToothNumber.Value)
            {
                return PatientImageErrors.ToothTreatmentMismatch;
            }
        }

        // 4. Persist under a server-generated name; the client file name is kept
        //    for display only and never touches the file system.
        buffer.Position = 0;
        var storagePath = await storage.SaveAsync(buffer, extension, cancellationToken);

        var safeFileName = Path.GetFileName(request.FileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            await storage.DeleteAsync(storagePath, cancellationToken);
            return PatientImageErrors.FileNameRequired;
        }

        if (safeFileName.Length > 255)
        {
            safeFileName = safeFileName[..255];
        }

        var result = PatientImage.Create(
            request.PatientId,
            request.TreatmentRecordId,
            request.ToothNumber,
            request.Kind,
            safeFileName,
            detectedContentType,
            buffer.Length,
            storagePath,
            user.Id,
            request.CapturedAtUtc ?? DateTimeOffset.UtcNow);

        if (result.IsError)
        {
            // Never leave an orphaned file behind when the row is rejected.
            await storage.DeleteAsync(storagePath, cancellationToken);
            logger.LogWarning("Failed to create patient image row. Errors: {@Errors}", result.Errors);
            return result.Errors;
        }

        var image = result.Value;

        context.PatientImages.Add(image);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("patient-image", cancellationToken);

        logger.LogInformation(
            "Patient image {ImageId} uploaded for patient {PatientId} by {UserId}.",
            image.Id,
            image.PatientId,
            user.Id);

        return image.ToDto();
    }
}