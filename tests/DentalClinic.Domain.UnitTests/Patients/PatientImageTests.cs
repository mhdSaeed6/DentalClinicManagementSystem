using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Domain.UnitTests.Patients;

public class PatientImageTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 11, 1, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldSucceed_WithValidInput()
    {
        var patientId = Guid.NewGuid();

        var result = PatientImage.Create(
            patientId,
            treatmentRecordId: null,
            toothNumber: 16,
            PatientImageKind.Xray,
            "  molar.png  ",
            " Image/PNG ",
            sizeBytes: 2048,
            "2026/11/molar.png",
            "  user-1  ",
            CapturedAt);

        Assert.True(result.IsSuccess);

        var image = result.Value;
        Assert.NotEqual(Guid.Empty, image.Id);
        Assert.Equal(patientId, image.PatientId);
        Assert.Null(image.TreatmentRecordId);
        Assert.Equal(16, image.ToothNumber);
        Assert.Equal(PatientImageKind.Xray, image.Kind);
        Assert.Equal("molar.png", image.FileName);
        Assert.Equal("image/png", image.ContentType);
        Assert.Equal(2048, image.SizeBytes);
        Assert.Equal("2026/11/molar.png", image.StoragePath);
        Assert.Equal("user-1", image.UploadedBy);
        Assert.Equal(CapturedAt, image.CapturedAtUtc);
    }

    [Fact]
    public void Create_ShouldKeepOptionalLinks_AndAllowNoTooth()
    {
        var treatmentRecordId = Guid.NewGuid();

        var result = PatientImage.Create(
            Guid.NewGuid(),
            treatmentRecordId,
            toothNumber: null,
            PatientImageKind.After,
            "smile.jpg",
            "image/jpeg",
            sizeBytes: 1024,
            "2026/11/smile.jpg",
            "user-1",
            CapturedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(treatmentRecordId, result.Value.TreatmentRecordId);
        Assert.Null(result.Value.ToothNumber);
        Assert.Equal(PatientImageKind.After, result.Value.Kind);
    }

    [Fact]
    public void Create_ShouldFail_WhenPatientIdIsEmpty()
    {
        var result = Create(patientId: Guid.Empty);

        Assert.Equal(PatientImageErrors.PatientIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenKindIsNotDefined()
    {
        var result = Create(kind: (PatientImageKind)99);

        Assert.Equal(PatientImageErrors.KindInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenFileNameIsBlank()
    {
        var result = Create(fileName: "   ");

        Assert.Equal(PatientImageErrors.FileNameRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenContentTypeIsBlank()
    {
        var result = Create(contentType: "   ");

        Assert.Equal(PatientImageErrors.UnsupportedContentType.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenSizeIsNotPositive()
    {
        Assert.Equal(
            PatientImageErrors.InvalidSize.Code,
            Create(sizeBytes: 0).TopError.Code);

        Assert.Equal(
            PatientImageErrors.InvalidSize.Code,
            Create(sizeBytes: -1).TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenStoragePathIsBlank()
    {
        var result = Create(storagePath: "  ");

        Assert.Equal(PatientImageErrors.StoragePathRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenUploaderIsBlank()
    {
        var result = Create(uploadedBy: "  ");

        Assert.Equal(PatientImageErrors.UploadedByRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WithInvalidToothNumber()
    {
        Assert.Equal(
            PatientImageErrors.InvalidToothNumber.Code,
            Create(toothNumber: 99).TopError.Code);

        Assert.Equal(
            PatientImageErrors.InvalidToothNumber.Code,
            Create(toothNumber: 0).TopError.Code);
    }

    private static Result<PatientImage> Create(
        Guid? patientId = null,
        PatientImageKind kind = PatientImageKind.Xray,
        string fileName = "molar.png",
        string contentType = "image/png",
        long sizeBytes = 2048,
        string storagePath = "2026/11/molar.png",
        string uploadedBy = "user-1",
        int? toothNumber = 16) =>
        PatientImage.Create(
            patientId ?? Guid.NewGuid(),
            treatmentRecordId: null,
            toothNumber,
            kind,
            fileName,
            contentType,
            sizeBytes,
            storagePath,
            uploadedBy,
            CapturedAt);
}