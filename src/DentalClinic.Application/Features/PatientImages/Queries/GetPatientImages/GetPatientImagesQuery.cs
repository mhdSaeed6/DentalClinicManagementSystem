using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImages;

public sealed record GetPatientImagesQuery(Guid PatientId, PatientAccessScope Scope)
    : ICachedQuery<Result<IReadOnlyList<PatientImageDto>>>
{
    public string CacheKey =>
        $"patient-images-{PatientId}-scope-{Scope.IsRestricted}-{Scope.DoctorId}-{Scope.UserId}";

    public string[] Tags => ["patient-image"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}