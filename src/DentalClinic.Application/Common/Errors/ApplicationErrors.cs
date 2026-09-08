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