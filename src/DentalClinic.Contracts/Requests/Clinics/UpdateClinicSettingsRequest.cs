namespace DentalClinic.Contracts.Requests.Clinics;

public class UpdateClinicSettingsRequest
{
    public string Name { get; set; } = null!;
    public string NameArabic { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string TimeZoneId { get; set; } = null!;
    public string BaseCurrency { get; set; } = null!;
    public string? SecondaryCurrency { get; set; }
    public string? DefaultCountryCode { get; set; }
}