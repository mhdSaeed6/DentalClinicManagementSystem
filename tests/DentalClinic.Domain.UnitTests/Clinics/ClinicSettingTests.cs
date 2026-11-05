using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Common.Results;

using Xunit;

namespace DentalClinic.Domain.UnitTests.Clinics;

public class ClinicSettingTests
{
    private static Result<ClinicSetting> Valid(
        string name = "Al Noor Dental Clinic",
        string nameArabic = "عيادة النور",
        string? timeZoneId = null,
        string baseCurrency = "usd",
        string? secondaryCurrency = null)
        => ClinicSetting.Create(
            name,
            nameArabic,
            logoUrl: null,
            phone: null,
            address: null,
            email: null,
            timeZoneId: timeZoneId ?? TimeZoneInfo.Local.Id,
            baseCurrency,
            secondaryCurrency,
            defaultCountryCode: null);

    [Fact]
    public void Create_WithValidData_NormalisesCurrencyAndTrimsText()
    {
        var result = Valid(name: "  Al Noor  ", baseCurrency: "usd");

        Assert.True(result.IsSuccess);
        Assert.Equal("Al Noor", result.Value.Name);
        Assert.Equal("USD", result.Value.BaseCurrency);
        Assert.Equal(ClinicSetting.SingletonId, result.Value.Id);
    }

    [Fact]
    public void Create_WithBlankName_ReturnsNameRequired()
    {
        var result = Valid(name: "   ");

        Assert.True(result.IsError);
        Assert.Equal(ClinicSettingErrors.NameRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithUnknownTimeZone_ReturnsInvalidTimeZone()
    {
        var result = Valid(timeZoneId: "Not/A_Real_Zone");

        Assert.True(result.IsError);
        Assert.Equal(ClinicSettingErrors.InvalidTimeZone.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithShortCurrency_ReturnsInvalidBaseCurrency()
    {
        var result = Valid(baseCurrency: "US");

        Assert.True(result.IsError);
        Assert.Equal(ClinicSettingErrors.InvalidBaseCurrency.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithSecondaryEqualToBase_ReturnsConflictError()
    {
        var result = Valid(baseCurrency: "USD", secondaryCurrency: "usd");

        Assert.True(result.IsError);
        Assert.Equal(ClinicSettingErrors.SecondaryCurrencyMatchesBase.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithDifferentSecondary_NormalisesBoth()
    {
        var result = Valid(baseCurrency: "usd", secondaryCurrency: "egp");

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Value.BaseCurrency);
        Assert.Equal("EGP", result.Value.SecondaryCurrency);
    }

    [Fact]
    public void Update_ReplacesValues()
    {
        var setting = Valid().Value;

        var result = setting.Update(
            name: "Smile Studio",
            nameArabic: "استوديو الابتسامة",
            logoUrl: "/branding/logo.svg",
            phone: "+20 100 000 0000",
            address: "Cairo",
            email: "hello@example.com",
            timeZoneId: TimeZoneInfo.Local.Id,
            baseCurrency: "egp",
            secondaryCurrency: "usd",
            defaultCountryCode: "20");

        Assert.True(result.IsSuccess);
        Assert.Equal("Smile Studio", setting.Name);
        Assert.Equal("EGP", setting.BaseCurrency);
        Assert.Equal("USD", setting.SecondaryCurrency);
        Assert.Equal("20", setting.DefaultCountryCode);
    }

    [Fact]
    public void Update_WithInvalidTimeZone_LeavesValuesUntouched()
    {
        var setting = Valid().Value;

        var result = setting.Update(
            name: "Smile Studio",
            nameArabic: "استوديو الابتسامة",
            logoUrl: null,
            phone: null,
            address: null,
            email: null,
            timeZoneId: "Not/A_Real_Zone",
            baseCurrency: "USD",
            secondaryCurrency: null,
            defaultCountryCode: null);

        Assert.True(result.IsError);
        Assert.Equal("Al Noor Dental Clinic", setting.Name);
    }

    [Fact]
    public void ResolveTimeZone_ForKnownId_ReturnsThatZone()
    {
        var setting = Valid(timeZoneId: TimeZoneInfo.Local.Id).Value;

        Assert.Equal(TimeZoneInfo.Local.Id, setting.ResolveTimeZone().Id);
    }
}