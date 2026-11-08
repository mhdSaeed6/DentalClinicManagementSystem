using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

namespace DentalClinic.Application.Features.PatientImages.Commands.UploadPatientImage;

public sealed record UploadPatientImageCommand(
    Guid PatientId,
    Guid? TreatmentRecordId,
    int? ToothNumber,
    PatientImageKind Kind,
    string FileName,
    string ContentType,
    long Length,
    Stream Content,
    DateTimeOffset? CapturedAtUtc) : IRequest<Result<PatientImageDto>>;