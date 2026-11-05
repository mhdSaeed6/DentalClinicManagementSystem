using DentalClinic.Web.Api;
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

    [Fact]
    public async Task EnsureLoadedAsync_UsesTheServerProfileOverConfiguration()
    {
        var source = new FakeSource(new ClinicSettingsDto
        {
            Name = "Server Clinic",
            NameArabic = "عيادة الخادم",
            Address = "Cairo",
            TimeZoneId = "Africa/Cairo",
            BaseCurrency = "EGP",
        });

        var branding = Branding(
            source,
            ("Clinic:Name", "Configured Clinic"),
            ("Clinic:Address", "Configured Address"));

        await branding.EnsureLoadedAsync();

        Assert.Equal("Server Clinic", branding.DisplayName(arabic: false));
        Assert.Equal("Cairo", branding.Address);
        Assert.Equal("EGP", branding.BaseCurrency);
        Assert.Equal("Africa/Cairo", branding.TimeZoneId);
    }

    [Fact]
    public async Task EnsureLoadedAsync_WhenTheApiIsUnavailable_KeepsTheConfigurationFallback()
    {
        var branding = Branding(new FakeSource(null), ("Clinic:Name", "Configured Clinic"));

        await branding.EnsureLoadedAsync();

        Assert.Equal("Configured Clinic", branding.DisplayName(arabic: false));
        Assert.Equal("USD", branding.BaseCurrency);
    }

    private static ClinicBranding Branding(params (string Key, string Value)[] settings)
        => Branding(new FakeSource(null), settings);

    private static ClinicBranding Branding(IClinicSettingsSource source, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        return new ClinicBranding(configuration, source);
    }

    private sealed class FakeSource(ClinicSettingsDto? settings) : IClinicSettingsSource
    {
        public Task<ClinicSettingsDto?> GetClinicSettingsAsync(CancellationToken ct = default)
            => Task.FromResult(settings);
    }
}