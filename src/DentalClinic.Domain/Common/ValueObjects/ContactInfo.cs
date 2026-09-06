using System.Text.RegularExpressions;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Common.ValueObjects;

public record ContactInfo
{
    private static readonly Regex PhoneRegex = new(@"^\+?\d{7,15}$", RegexOptions.Compiled);

    public string PrimaryPhone { get; init; } = default!;
    public string? SecondaryPhone { get; init; }
    public bool HasWhatsAppOnPrimary { get; init; }
    public string? SocialMediaLink { get; init; } // صفحة فيسبوك/إنستغرام (خاصة بالطبيب)

    private ContactInfo() { }

    private ContactInfo(
        string primaryPhone,
        string? secondaryPhone,
        bool hasWhatsAppOnPrimary,
        string? socialMediaLink)
    {
        PrimaryPhone = primaryPhone;
        SecondaryPhone = secondaryPhone;
        HasWhatsAppOnPrimary = hasWhatsAppOnPrimary;
        SocialMediaLink = socialMediaLink;
    }

    public static Result<ContactInfo> Create(
        string primaryPhone,
        string? secondaryPhone = null,
        bool hasWhatsAppOnPrimary = true,
        string? socialMediaLink = null)
    {
        if (string.IsNullOrWhiteSpace(primaryPhone) || !PhoneRegex.IsMatch(primaryPhone))
        {
            return ContactInfoErrors.InvalidPhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(secondaryPhone) && !PhoneRegex.IsMatch(secondaryPhone))
        {
            return ContactInfoErrors.InvalidPhoneNumber;
        }

        return new ContactInfo(
            primaryPhone.Trim(),
            string.IsNullOrWhiteSpace(secondaryPhone) ? null : secondaryPhone.Trim(),
            hasWhatsAppOnPrimary,
            string.IsNullOrWhiteSpace(socialMediaLink) ? null : socialMediaLink.Trim());
    }
}

