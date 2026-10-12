namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Trusted patient scope applied to authorization-sensitive queries. When
/// <see cref="IsRestricted"/> is <see langword="true"/> the query must be filtered
/// to patients created by <see cref="UserId"/> or registered to <see cref="DoctorId"/>.
/// </summary>
public readonly record struct PatientAccessScope(bool IsRestricted, Guid? DoctorId, string? UserId)
{
    /// <summary>Unrestricted scope (administrators and receptionists).</summary>
    public static PatientAccessScope None => new(false, null, null);
}
