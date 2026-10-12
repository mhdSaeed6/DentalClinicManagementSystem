namespace DentalClinic.Application.Common.Interfaces;

public interface IUser
{
    /// <summary>
    /// Identifier of the authenticated identity account, derived from a trusted
    /// claim. Null when the request is unauthenticated.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Identifier of the <c>Doctor</c> profile linked to the authenticated account,
    /// derived from a trusted server-issued claim. Null for accounts that are not
    /// linked to a doctor profile (e.g. administrators and receptionists), and for
    /// unauthenticated requests.
    /// </summary>
    Guid? DoctorId { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the authenticated account is a member of
    /// the supplied role. Always <see langword="false"/> for unauthenticated requests.
    /// </summary>
    bool IsInRole(string role);
}
