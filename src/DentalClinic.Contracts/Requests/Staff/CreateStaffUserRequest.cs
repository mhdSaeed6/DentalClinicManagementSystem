using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Staff;

public sealed class CreateStaffUserRequest
{
    /// <summary>Email is also used as the login user name.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Must satisfy the clinic password policy: 12+ chars with digits, upper, lower and symbols.</summary>
    [Required]
    [MinLength(12)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Role names from the clinic vocabulary: Admin, Doctor, Receptionist.</summary>
    [Required]
    public List<string> Roles { get; set; } = [];
}