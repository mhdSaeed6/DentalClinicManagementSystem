using System.ComponentModel.DataAnnotations;

using DentalClinic.Contracts.Requests.Doctors;

namespace DentalClinic.Contracts.Requests.Patients;

public class CreatePatientRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be either 'Male' or 'Female'.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact information is required.")]
    public CreateContactInfoRequest ContactInfo { get; set; } = default!;

    [Required(ErrorMessage = "Medical history is required.")]
    public MedicalHistoryRequest MedicalHistory { get; set; } = default!;
}
