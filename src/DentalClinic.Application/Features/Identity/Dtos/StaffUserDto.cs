namespace DentalClinic.Application.Features.Identity.Dtos;

/// <summary>
/// Read-back DTO for the staff-administration surface. Carries every field the
/// "Staff" screen needs in one round trip: identity state (roles, lockout) and
/// the linked doctor profile, when the account is a clinician.
/// </summary>
public sealed record StaffUserDto(
    string UserId,
    string Email,
    string? UserName,
    IReadOnlyList<string> Roles,
    bool IsLockedOut)
{
    /// <summary>Doctor profile linked to this account, or <c>null</c>.</summary>
    public Guid? LinkedDoctorId { get; init; }

    /// <summary>Display name of the linked doctor profile, or <c>null</c>.</summary>
    public string? LinkedDoctorName { get; init; }
}