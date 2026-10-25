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
            durationMinutes: 45, doctorName: "Sara Khaled");

        Assert.Contains("Alia Hassan", message);
        Assert.Contains("Dr. Sara Khaled", message);
        Assert.Contains("Al Noor Dental", message);
        Assert.Contains("02 Nov 2026", message);
    }

    [Fact]
    public void AppointmentReminderMessage_ArabicContainsPatientDoctorAndClinic()
    {
        var message = WhatsAppMessageBuilder.AppointmentReminderMessage(
            arabic: true, patientName: "عليا حسن", scheduledDateTime: new DateTime(2026, 11, 2, 10, 0, 0),
            durationMinutes: 45, doctorName: "سارة خالد");

        Assert.Contains("عليا حسن", message);
        Assert.Contains("د. سارة خالد", message);
        Assert.Contains("عيادة النور", message);
    }

    [Fact]
    public void AppointmentReminderMessage_FallsBackWhenDoctorUnknown()
    {
        var message = WhatsAppMessageBuilder.AppointmentReminderMessage(
            arabic: false, patientName: "Alia", scheduledDateTime: new DateTime(2026, 11, 2, 10, 0, 0),
            durationMinutes: 30, doctorName: " ");

        Assert.Contains("with Dr.", message);
        Assert.DoesNotContain("Dr.  ", message); // no doubled space from an empty name
    }

    [Fact]
    public void GeneralContactMessage_ContainsPatientAndClinicInBothLanguages()
    {
        var english = WhatsAppMessageBuilder.GeneralContactMessage(arabic: false, patientName: "Alia");
        var arabic = WhatsAppMessageBuilder.GeneralContactMessage(arabic: true, patientName: "عليا");

        Assert.Contains("Alia", english);
        Assert.Contains("Al Noor Dental", english);
        Assert.Contains("عليا", arabic);
        Assert.Contains("عيادة النور", arabic);
    }
}
