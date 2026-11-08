using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Patients;

public static class PatientImageErrors
{
    public static readonly Error PatientIdRequired = Error.Validation(
        "PatientImage.PatientIdRequired",
        "Patient ID is required.");

    public static readonly Error KindInvalid = Error.Validation(
        "PatientImage.KindInvalid",
        "The image kind is not a valid category.");

    public static readonly Error FileNameRequired = Error.Validation(
        "PatientImage.FileNameRequired",
        "The image file name is required.");

    public static readonly Error UnsupportedContentType = Error.Validation(
        "PatientImage.UnsupportedContentType",
        "Only JPEG, PNG and WebP images are accepted.");

    public static readonly Error InvalidSize = Error.Validation(
        "PatientImage.InvalidSize",
        "The image size must be greater than zero.");

    public static readonly Error StoragePathRequired = Error.Validation(
        "PatientImage.StoragePathRequired",
        "The stored image path is required.");

    public static readonly Error UploadedByRequired = Error.Validation(
        "PatientImage.UploadedByRequired",
        "The uploading account is required.");

    public static readonly Error InvalidToothNumber = Error.Validation(
        "PatientImage.InvalidToothNumber",
        "Tooth number must be a valid dental chart number (1-32 or 51-85).");

    public static readonly Error ToothTreatmentMismatch = Error.Validation(
        "PatientImage.ToothTreatmentMismatch",
        "The tooth number does not match the linked treatment record.");

    public static readonly Error FileTooLarge = Error.Validation(
        "PatientImage.FileTooLarge",
        "Images must be 10 MB or smaller.");

    public static readonly Error UnsupportedFormat = Error.Validation(
        "PatientImage.UnsupportedFormat",
        "The file content is not a valid JPEG, PNG or WebP image.");

    public static readonly Error ContentMissing = Error.NotFound(
        "PatientImage.ContentMissing",
        "The stored image file could not be found.");

    public static readonly Error FileRequired = Error.Validation(
        "PatientImage.FileRequired",
        "An image file is required.");

    public static readonly Error KindRequired = Error.Validation(
        "PatientImage.KindRequired",
        "The image kind is required.");
}