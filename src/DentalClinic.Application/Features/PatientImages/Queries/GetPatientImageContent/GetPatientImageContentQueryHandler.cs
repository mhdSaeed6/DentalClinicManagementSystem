using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImageContent;

public class GetPatientImageContentQueryHandler(IAppDbContext context, IImageStorage storage)
    : IRequestHandler<GetPatientImageContentQuery, Result<PatientImageContent>>
{
    public async Task<Result<PatientImageContent>> Handle(GetPatientImageContentQuery request, CancellationToken cancellationToken)
    {
        var visiblePatient = context.Patients
            .Where(p => p.Id == request.PatientId)
            .ApplyScope(context, request.Scope);

        if (!await visiblePatient.AnyAsync(cancellationToken))
        {
            return ApplicationErrors.PatientNotFound;
        }

        var image = await context.PatientImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ImageId && i.PatientId == request.PatientId, cancellationToken);

        if (image is null)
        {
            return ApplicationErrors.PatientImageNotFound;
        }

        var content = await storage.OpenReadAsync(image.StoragePath, cancellationToken);

        if (content is null)
        {
            return PatientImageErrors.ContentMissing;
        }

        return new PatientImageContent(content, image.ContentType, image.FileName);
    }
}