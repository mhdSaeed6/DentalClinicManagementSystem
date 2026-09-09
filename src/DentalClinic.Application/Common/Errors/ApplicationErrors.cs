using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Common.Errors;

public static class ApplicationErrors
{
    public static Error PatientNotFound =>
        Error.NotFound(
            code: "Patient.NotFound",
            description: "Patient not found.");
    public static Error PatientHasAppointments =>
        Error.Conflict(
            code: "Patient.HasAppointments",
            description: "Patient has associated appointments.");

    public static Error DoctorInactive =>
        Error.Conflict(
            code: "Doctor.Inactive",
            description: "Doctor is inactive.");
    public static Error DoctorNotFound =>
        Error.NotFound(
            code: "Doctor.NotFound",
            description: "Doctor not found.");

    public static Error ServiceNotFound =>
        Error.NotFound(
            code: "Service.NotFound",
            description: "Service not found.");

    public static Error AppointmentNotFound =>
        Error.NotFound(
            code: "Appointment.NotFound",
            description: "Appointment not found.");

    public static Error InvoiceNotFound =>
        Error.NotFound(
            code: "Invoice.NotFound",
            description: "Invoice not found.");

    public static Error TreatmentRecordNotFound =>
        Error.NotFound(
            code: "TreatmentRecord.NotFound",
            description: "Treatment record not found.");

    public static Error InvalidRefreshToken =>
    Error.Validation(
        "RefreshToken.Expiry.Invalid",
        "Expiry must be in the future.");

    public static readonly Error ExpiredAccessTokenInvalid = Error.Conflict(
         code: "Auth.ExpiredAccessToken.Invalid",
         description: "Expired access token is not valid.");

    public static readonly Error UserIdClaimInvalid = Error.Conflict(
        code: "Auth.UserIdClaim.Invalid",
        description: "Invalid userId claim.");

    public static readonly Error RefreshTokenExpired = Error.Conflict(
        code: "Auth.RefreshToken.Expired",
        description: "Refresh token is invalid or has expired.");

    public static readonly Error UserNotFound = Error.NotFound(
        code: "Auth.User.NotFound",
        description: "User not found.");

    public static readonly Error TokenGenerationFailed = Error.Failure(
        code: "Auth.TokenGeneration.Failed",
        description: "Failed to generate new JWT token.");
}