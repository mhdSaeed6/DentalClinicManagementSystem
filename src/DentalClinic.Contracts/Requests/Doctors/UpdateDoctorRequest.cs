using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Doctors;

public class UpdateDoctorRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specialization is required.")]
    [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
    public string Specialization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact information is required.")]
    public CreateContactInfoRequest ContactInfo { get; set; } = default!;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be either 'Male' or 'Female'.")]
    public string Gender { get; set; } = string.Empty;
}
