using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Doctors;

public class CreateContactInfoRequest
{
    [Required(ErrorMessage = "Primary phone number is required.")]
    [RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Primary phone number must be 7–15 digits and may start with '+'.")]
    public string PrimaryPhone { get; set; } = string.Empty;

    [RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Secondary phone number must be 7–15 digits and may start with '+'.")]
    public string? SecondaryPhone { get; set; }

    public bool HasWhatsAppOnPrimary { get; set; } = true;

    [Url(ErrorMessage = "Social media link must be a valid URL.")]
    public string? SocialMediaLink { get; set; }
}