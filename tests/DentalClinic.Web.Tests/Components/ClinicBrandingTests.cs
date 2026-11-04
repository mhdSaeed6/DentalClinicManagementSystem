using DentalClinic.Web.Components;

using Microsoft.Extensions.Configuration;

namespace DentalClinic.Web.Tests.Components;

public sealed class ClinicBrandingTests
{
    [Fact]
    public void DisplayName_WithoutConfiguration_FallsBackToTheBuiltInClinic()
    {
        var branding = Branding();

        Assert.Equal("Al Noor Dental Clinic", branding.DisplayName(arabic: false));
        Assert.Equal("عيادة النور لطب الأسنان", branding.DisplayName(arabic: true));
    }

    [Fact]
    public void DisplayName_WithConfiguration_UsesTheConfiguredClinic()
    {
        var branding = Branding(
            ("Clinic:Name", "Smile Studio"),
            ("Clinic:NameArabic", "استوديو الابتسامة"));

        Assert.Equal("Smile Studio", branding.DisplayName(arabic: false));
        Assert.Equal("استوديو الابتسامة", branding.DisplayName(arabic: true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DisplayName_BlankConfiguration_IsIgnored(string value)
    {
        // A blank entry must not blank out the printed header.
        var branding = Branding(("Clinic:Name", value));

        Assert.Equal("Al Noor Dental Clinic", branding.DisplayName(arabic: false));
    }

    [Fact]
    public void OptionalContactDetails_AreNullWhenNotConfigured()
    {
        var branding = Branding();

        Assert.Null(branding.Address);
        Assert.Null(branding.Phone);
        Assert.Null(branding.Email);
    }

    [Fact]
    public void OptionalContactDetails_AreTrimmedWhenConfigured()
    {
        var branding = Branding(
            ("Clinic:Address", "  12 Nile Street  "),
            ("Clinic:Phone", " +20 100 000 0000 "),
            ("Clinic:Email", " front@clinic.test "));

        Assert.Equal("12 Nile Street", branding.Address);
        Assert.Equal("+20 100 000 0000", branding.Phone);
        Assert.Equal("front@clinic.test", branding.Email);
    }

    private static ClinicBranding Branding(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        return new ClinicBranding(configuration);
    }
}