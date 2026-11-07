using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Domain.Patients;

/// <summary>
/// A clinical image (X-ray, before/after photo) attached to a patient. The
/// binary lives outside the database: <see cref="StoragePath"/> is opaque to the
/// domain and resolved by the storage provider. Only server-generated paths are
/// ever stored - the client's file name is kept for display only.
/// </summary>
public class PatientImage : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid? TreatmentRecordId { get; private set; }

    // Nullable on purpose (S8 convention): a full-mouth X-ray is not tied to a
    // single tooth; a provided value must still be a valid FDI tooth.
    public int? ToothNumber { get; private set; }

    public PatientImageKind Kind { get; private set; }

    /// <summary>The original client file name, shown in the UI only.</summary>
    public string FileName { get; private set; } = default!;

    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }

    /// <summary>Server-generated, provider-specific locator for the binary.</summary>
    public string StoragePath { get; private set; } = default!;

    public string UploadedBy { get; private set; } = default!;
    public DateTimeOffset CapturedAtUtc { get; private set; }

    public Patient? Patient { get; set; }
    public TreatmentRecord? TreatmentRecord { get; set; }

    private PatientImage() { }

    private PatientImage(
        Guid id,
        Guid patientId,
        Guid? treatmentRecordId,
        int? toothNumber,
        PatientImageKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storagePath,
        string uploadedBy,
        DateTimeOffset capturedAtUtc)
        : base(id)
    {
        PatientId = patientId;
        TreatmentRecordId = treatmentRecordId;
        ToothNumber = toothNumber;
        Kind = kind;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StoragePath = storagePath;
        UploadedBy = uploadedBy;
        CapturedAtUtc = capturedAtUtc;
    }

    public static Result<PatientImage> Create(
        Guid patientId,
        Guid? treatmentRecordId,
        int? toothNumber,
        PatientImageKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storagePath,
        string uploadedBy,
        DateTimeOffset capturedAtUtc)
    {
        if (patientId == Guid.Empty)
        {
            return PatientImageErrors.PatientIdRequired;
        }

        if (!Enum.IsDefined(kind))
        {
            return PatientImageErrors.KindInvalid;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return PatientImageErrors.FileNameRequired;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return PatientImageErrors.UnsupportedContentType;
        }

        if (sizeBytes <= 0)
        {
            return PatientImageErrors.InvalidSize;
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return PatientImageErrors.StoragePathRequired;
        }

        if (string.IsNullOrWhiteSpace(uploadedBy))
        {
            return PatientImageErrors.UploadedByRequired;
        }

        if (toothNumber.HasValue && !FdiTooth.IsValidToothNumber(toothNumber.Value))
        {
            return PatientImageErrors.InvalidToothNumber;
        }

        return new PatientImage(
            Guid.NewGuid(),
            patientId,
            treatmentRecordId,
            toothNumber,
            kind,
            fileName.Trim(),
            contentType.Trim().ToLowerInvariant(),
            sizeBytes,
            storagePath,
            uploadedBy.Trim(),
            capturedAtUtc);
    }
}