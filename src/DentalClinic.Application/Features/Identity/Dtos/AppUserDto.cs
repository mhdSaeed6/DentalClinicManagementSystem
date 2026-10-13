using System.Security.Claims;

namespace DentalClinic.Application.Features.Identity.Dtos;

public sealed record AppUserDto(string UserId, string Email, IList<string> Roles, IList<Claim> Claims)
{
    /// <summary>
    /// Identifier of the doctor profile linked to this account, when the account
    /// belongs to a doctor. Populated by the user-info query so clients can scope
    /// doctor-facing screens to the signed-in clinician.
    /// </summary>
    public Guid? DoctorId { get; init; }
}