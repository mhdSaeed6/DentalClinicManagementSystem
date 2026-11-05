using DentalClinic.Web.Components;

namespace DentalClinic.Web.Tests.Components;

public class ClinicFormattingTests
{
    [Fact]
    public void ToothLabel_ShouldReturnDash_WhenToothNumberIsNull()
    {
        // S8: a treatment recorded without a tooth shows an em dash, not a fake tooth.
        Assert.Equal("—", ClinicFormatting.ToothLabel(null));
    }

    [Theory]
    [InlineData(26, "UL·6")]
    [InlineData(36, "LL·6")]
    [InlineData(11, "UR·1")]
    public void ToothLabel_ShouldSegmentQuadrants_TheSameForNullableAndPlainValues(int toothNumber, string expected)
    {
        Assert.Equal(expected, ClinicFormatting.ToothLabel(toothNumber));
        Assert.Equal(expected, ClinicFormatting.ToothLabel((int?)toothNumber));
    }

    [Fact]
    public void Money_ShouldUseTheSuppliedCurrency_NotAHardcodedOne()
    {
        Assert.Equal("1,234.50 USD", ClinicFormatting.Money(1234.5m, "USD"));
        Assert.Equal("1,234.50 EGP", ClinicFormatting.Money(1234.5m, "EGP"));
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("IQD")]
    [InlineData("KRW")]
    public void Money_ShouldDropTheMinorUnit_ForZeroDecimalCurrencies(string currency)
    {
        // "1,200.00 IQD" claims a precision the currency does not have.
        Assert.Equal("1,200 " + currency, ClinicFormatting.Money(1200m, currency));
    }

    [Fact]
    public void Money_ShouldNormaliseTheCurrencyCode()
    {
        Assert.Equal("10.00 USD", ClinicFormatting.Money(10m, " usd "));
    }

    [Fact]
    public void Money_ShouldOmitTheSuffix_WhenNoCurrencyIsConfigured()
    {
        Assert.Equal("10.00", ClinicFormatting.Money(10m, string.Empty));
    }
}