namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Trusted doctor scope applied to authorization-sensitive queries. When
/// <see cref="IsRestricted"/> is <see langword="true"/> the query must be filtered
/// to <see cref="DoctorId"/> (a value of <see cref="Guid.Empty"/> matches nothing,
/// which fails closed for accounts without a linked doctor profile).
/// </summary>
public readonly record struct DoctorAccessScope(bool IsRestricted, Guid DoctorId)
{
    /// <summary>Unrestricted scope (administrators and receptionists).</summary>
    public static DoctorAccessScope None => new(false, Guid.Empty);
}
