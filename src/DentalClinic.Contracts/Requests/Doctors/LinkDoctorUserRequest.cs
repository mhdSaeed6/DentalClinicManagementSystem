using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Doctors;

public sealed class LinkDoctorUserRequest
{
    /// <summary>Identity user id (AspNetUsers.Id) of the doctor account.</summary>
    [Required]
    [MaxLength(64)]
    public string UserId { get; set; } = string.Empty;
}