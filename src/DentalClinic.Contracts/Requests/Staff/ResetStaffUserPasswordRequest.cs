using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Staff;

public sealed class ResetStaffUserPasswordRequest
{
    [Required]
    [MinLength(12)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}