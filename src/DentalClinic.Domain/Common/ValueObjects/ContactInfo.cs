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
        PrimaryPhone = primaryPhone.Trim();
        SecondaryPhone = secondaryPhone?.Trim();
        HasWhatsAppOnPrimary = hasWhatsAppOnPrimary;
        SocialMediaLink = socialMediaLink?.Trim();
    }

    public static Result<ContactInfo> Create(
        string primaryPhone,
        string? secondaryPhone = null,
        bool hasWhatsAppOnPrimary = true,
        string? socialMediaLink = null)
    {
        var trimmedPrimary = primaryPhone?.Trim();
        var trimmedSecondary = string.IsNullOrWhiteSpace(secondaryPhone) ? null : secondaryPhone.Trim();
        var trimmedSocial = string.IsNullOrWhiteSpace(socialMediaLink) ? null : socialMediaLink.Trim();

        if (string.IsNullOrWhiteSpace(trimmedPrimary) || !PhoneRegex.IsMatch(trimmedPrimary))
        {
            return ContactInfoErrors.InvalidPhoneNumber;
        }

        if (trimmedSecondary is not null && !PhoneRegex.IsMatch(trimmedSecondary))
        {
            return ContactInfoErrors.InvalidPhoneNumber;
        }

        return new ContactInfo(
            trimmedPrimary,
            trimmedSecondary,
            hasWhatsAppOnPrimary,
            trimmedSocial);
    }
}

