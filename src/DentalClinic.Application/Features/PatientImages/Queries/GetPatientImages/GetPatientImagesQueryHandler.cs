using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Application.Features.PatientImages.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImages;

public class GetPatientImagesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPatientImagesQuery, Result<IReadOnlyList<PatientImageDto>>>
{
    public async Task<Result<IReadOnlyList<PatientImageDto>>> Handle(GetPatientImagesQuery request, CancellationToken cancellationToken)
    {
        // Resolve the patient through the same trusted scope used by the
        // registry, so an image can never widen a caller's reach.
        var visiblePatient = context.Patients
            .Where(p => p.Id == request.PatientId)
            .ApplyScope(context, request.Scope);

        if (!await visiblePatient.AnyAsync(cancellationToken))
        {
            return ApplicationErrors.PatientNotFound;
        }

        var images = await context.PatientImages
            .AsNoTracking()
            .Where(i => i.PatientId == request.PatientId)
            .OrderByDescending(i => i.CapturedAtUtc)
            .ThenByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return images.ToDtos();
    }
}