namespace DentalClinic.Domain.Identity;

/// <summary>
/// Custom JWT claim types used to carry clinic-specific, server-trusted identity
/// data. Values are issued by the token provider and must never be supplied by
/// the client.
/// </summary>
public static class ClinicClaimTypes
{
    /// <summary>
    /// Identifier of the <c>Doctor</c> profile linked to the authenticated account.
    /// Absent for accounts that are not linked to a doctor profile.
    /// </summary>
    public const string DoctorId = "doctor_id";
}
