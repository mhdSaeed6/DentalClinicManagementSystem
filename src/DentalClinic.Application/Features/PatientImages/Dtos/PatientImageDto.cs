using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Features.PatientImages.Dtos;

/// <summary>
/// Metadata for one stored clinical image. The binary is fetched through the
/// dedicated content endpoint so list responses stay small.
/// </summary>
public sealed record PatientImageDto(
    Guid Id,
    Guid PatientId,
    Guid? TreatmentRecordId,
    int? ToothNumber,
    PatientImageKind Kind,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CapturedAtUtc,
    string UploadedBy,
    DateTimeOffset CreatedAtUtc);

/// <summary>Open binary plus the headers needed to serve it inline.</summary>
public sealed record PatientImageContent(Stream Content, string ContentType, string FileName);