namespace DentalClinic.Web.Tests.Components;

using DentalClinic.Web.Components;

public class WhatsAppMessageBuilderTests
{
    // ------------------------------------------------------ TryNormalize

    [Theory]
    [InlineData("+963 944 123 456", "963944123456")]
    [InlineData("(011) 944-123-456", "011944123456")]
    [InlineData("963944123456", "963944123456")]
    public void TryNormalize_ConcentratesDigits(string input, string expected)
    {
        Assert.Equal(expected, WhatsAppMessageBuilder.TryNormalize(input));
    }

    [Theory]
    [InlineData("00963 944 123 456", "963944123456")] // international dial prefix, not a country code
    [InlineData("00963944123456", "963944123456")]
    public void TryNormalize_StripsLeadingDoubleZero(string input, string expected)
    {
        Assert.Equal(expected, WhatsAppMessageBuilder.TryNormalize(input));
    }

    [Theory]
    [InlineData("0999 123 456", "963999123456")] // local form: drop the zero, add the clinic's code
    [InlineData("+963 999 123 456", "963999123456")] // already international: left alone
    [InlineData("00963 999 123 456", "963999123456")] // 00 prefix: left alone after stripping
    [InlineData("0999123456", "963999123456")]
    public void TryNormalize_WithTheClinicCountryCode_ResolvesLocalNumbers(string input, string expected)
    {
        Assert.Equal(expected, WhatsAppMessageBuilder.TryNormalize(input, "963"));
    }

    [Fact]
    public void TryNormalize_WithAnInvalidCountryCode_LeavesTheNumberUntouched()
    {
        // A code that cannot be a country code must not be prefixed; the naive
        // result is still the previous behaviour.
        Assert.Equal("0999123456", WhatsAppMessageBuilder.TryNormalize("0999 123 456", "000"));
        Assert.Equal("0999123456", WhatsAppMessageBuilder.TryNormalize("0999 123 456", " "));
        Assert.Equal("0999123456", WhatsAppMessageBuilder.TryNormalize("0999 123 456", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("call me")] // no digits at all
    [InlineData("123456")] // fewer than 7 digits
    [InlineData("12ab34")] // digits left after stripping punctuation
    [InlineData("1234567890123456")] // more than 15 digits
    public void TryNormalize_RejectsUnusableNumbers(string? input)
    {
        Assert.Null(WhatsAppMessageBuilder.TryNormalize(input));
    }

    [Theory]
    [InlineData("+963 944 123 456", true)]
    [InlineData("123456", false)]
    public void CanSend_MatchesNormalization(string input, bool expected)
    {
        // The availability check and the link builder cannot disagree.
        Assert.Equal(expected, WhatsAppMessageBuilder.CanSend(input));
        Assert.Equal(
            WhatsAppMessageBuilder.CanSend(input),
            WhatsAppMessageBuilder.TryNormalize(input) is not null);
    }

    [Fact]
    public void CanSend_WithACountryCode_AcceptsALocalNumber()
    {
        Assert.True(WhatsAppMessageBuilder.CanSend("0999 123 456", "963"));
    }

    // ------------------------------------------------------ TryBuildUrl

    [Fact]
    public void TryBuildUrl_BuildsWaMeLinkWithEncodedMessage()
    {
        const string message = "مرحباً عليا"; // Arabic must survive URL encoding

        var url = WhatsAppMessageBuilder.TryBuildUrl("+963 944 123 456", message);

        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/963944123456?text=", url);
        Assert.Equal(message, Uri.UnescapeDataString(url["https://wa.me/963944123456?text=".Length..]));
    }

    [Fact]
    public void TryBuildUrl_WithTheCountryCode_LinksTheResolvedNumber()
    {
        var url = WhatsAppMessageBuilder.TryBuildUrl("0999 123 456", "hello", "963");

        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/963999123456?text=", url);
    }

    [Fact]
    public void TryBuildUrl_PreservesLineBreaks()
    {
        var url = WhatsAppMessageBuilder.TryBuildUrl("+963944123456", "line one\nline two");

        Assert.NotNull(url);

        // \n is encoded, not swallowed, and decodes back to the original message.
        Assert.Contains("%0A", url);
        Assert.Contains("line two", Uri.UnescapeDataString(url));
    }

    [Fact]
    public void TryBuildUrl_ReturnsNullWhenPhoneUnusable()
    {
        Assert.Null(WhatsAppMessageBuilder.TryBuildUrl("12345", "hello"));
    }

    // ------------------------------------------------------ Message bodies

    [Fact]
    public void AppointmentReminderMessage_EnglishContainsPatientDoctorAndClinic()
    {
        var message = WhatsAppMessageBuilder.AppointmentReminderMessage(
            arabic: false, patientName: "Alia Hassan", scheduledDateTime: new DateTime(2026, 11, 2, 10, 0, 0),
            durationMinutes: 45, doctorName: "Sara Khaled",
            clinicName: "Smile Studio", clinicNameArabic: "استوديو الابتسامة");

        Assert.Contains("Alia Hassan", message);
        Assert.Contains("Dr. Sara Khaled", message);
        Assert.Contains("Smile Studio", message);
        Assert.Contains("02 Nov 2026", message);

        // The clinic's own name, never a hardcoded one.
        Assert.DoesNotContain("Al Noor", message);
    }

    [Fact]
    public void AppointmentReminderMessage_ArabicContainsPatientDoctorAndClinic()
    {
        var message = WhatsAppMessageBuilder.AppointmentReminderMessage(
            arabic: true, patientName: "عليا حسن", scheduledDateTime: new DateTime(2026, 11, 2, 10, 0, 0),
            durationMinutes: 45, doctorName: "سارة خالد",
            clinicName: "Smile Studio", clinicNameArabic: "استوديو الابتسامة");

        Assert.Contains("عليا حسن", message);
        Assert.Contains("د. سارة خالد", message);
        Assert.Contains("استوديو الابتسامة", message);
    }

    [Fact]
    public void AppointmentReminderMessage_FallsBackWhenDoctorUnknown()
    {
        var message = WhatsAppMessageBuilder.AppointmentReminderMessage(
            arabic: false, patientName: "Alia", scheduledDateTime: new DateTime(2026, 11, 2, 10, 0, 0),
            durationMinutes: 30, doctorName: " ",
            clinicName: "Smile Studio", clinicNameArabic: "استوديو الابتسامة");

        Assert.Contains("with Dr.", message);
        Assert.DoesNotContain("Dr.  ", message); // no doubled space from an empty name
    }

    [Fact]
    public void MessageBodies_WithNoConfiguredClinicName_StayReadable()
    {
        // The profile may not have loaded yet; a message must never show a gap
        // where the clinic name belongs.
        var english = WhatsAppMessageBuilder.GeneralContactMessage(
            arabic: false, patientName: "Alia", clinicName: " ", clinicNameArabic: string.Empty);
        var arabic = WhatsAppMessageBuilder.GeneralContactMessage(
            arabic: true, patientName: "عليا", clinicName: " ", clinicNameArabic: string.Empty);

        Assert.Contains("the clinic", english);
        Assert.Contains("العيادة", arabic);
    }

    [Fact]
    public void GeneralContactMessage_ContainsPatientAndClinicInBothLanguages()
    {
        var english = WhatsAppMessageBuilder.GeneralContactMessage(
            arabic: false, patientName: "Alia", clinicName: "Smile Studio", clinicNameArabic: "استوديو الابتسامة");
        var arabic = WhatsAppMessageBuilder.GeneralContactMessage(
            arabic: true, patientName: "عليا", clinicName: "Smile Studio", clinicNameArabic: "استوديو الابتسامة");

        Assert.Contains("Alia", english);
        Assert.Contains("Smile Studio", english);
        Assert.Contains("عليا", arabic);
        Assert.Contains("استوديو الابتسامة", arabic);
    }
}
