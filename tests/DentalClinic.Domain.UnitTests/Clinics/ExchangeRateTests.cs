using DentalClinic.Domain.Clinics;

using Xunit;

namespace DentalClinic.Domain.UnitTests.Clinics;

public class ExchangeRateTests
{
    private static readonly DateTimeOffset Jan1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_NormalisesCurrencies()
    {
        var result = ExchangeRate.Create("usd", "egp", 48.5m, Jan1);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Value.FromCurrency);
        Assert.Equal("EGP", result.Value.ToCurrency);
        Assert.Equal(48.5m, result.Value.Rate);
        Assert.Equal(Jan1, result.Value.EffectiveFromUtc);
    }

    [Fact]
    public void Create_WithShortCurrency_ReturnsInvalidCurrency()
    {
        var result = ExchangeRate.Create("US", "EGP", 48.5m, Jan1);

        Assert.True(result.IsError);
        Assert.Equal(ExchangeRateErrors.InvalidCurrency.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithNonLetterCurrency_ReturnsInvalidCurrency()
    {
        var result = ExchangeRate.Create("US1", "EGP", 48.5m, Jan1);

        Assert.True(result.IsError);
        Assert.Equal(ExchangeRateErrors.InvalidCurrency.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithSameCurrency_ReturnsSameCurrency_IgnoringCase()
    {
        var result = ExchangeRate.Create("USD", "usd", 1m, Jan1);

        Assert.True(result.IsError);
        Assert.Equal(ExchangeRateErrors.SameCurrency.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveRate_ReturnsInvalidRate(decimal rate)
    {
        var result = ExchangeRate.Create("USD", "EGP", rate, Jan1);

        Assert.True(result.IsError);
        Assert.Equal(ExchangeRateErrors.InvalidRate.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_WithoutEffectiveDate_ReturnsInvalidEffectiveDate()
    {
        var result = ExchangeRate.Create("USD", "EGP", 48.5m, default);

        Assert.True(result.IsError);
        Assert.Equal(ExchangeRateErrors.InvalidEffectiveDate.Code, result.TopError.Code);
    }

    [Fact]
    public void PickLatest_ReturnsNewestRateInForceAtTheInstant()
    {
        var older = Create(rate: 48m, effectiveFromUtc: Jan1);
        var newer = Create(rate: 52m, effectiveFromUtc: Jan1.AddMonths(1));

        var picked = ExchangeRate.PickLatest([older, newer], Jan1.AddDays(10));

        // The February row is not in force yet in January.
        Assert.Same(older, picked);
    }

    [Fact]
    public void PickLatest_WhenInstantIsOnTheEffectiveDate_IncludesThatRow()
    {
        var rate = Create(effectiveFromUtc: Jan1);

        Assert.Same(rate, ExchangeRate.PickLatest([rate], Jan1));
    }

    [Fact]
    public void PickLatest_ReturnsNull_WhenHistoryStartsAfterTheInstant()
    {
        var rate = Create(effectiveFromUtc: Jan1);

        Assert.Null(ExchangeRate.PickLatest([rate], Jan1.AddDays(-1)));
    }

    [Fact]
    public void PickLatest_ReturnsNull_ForEmptyHistory()
    {
        Assert.Null(ExchangeRate.PickLatest([], Jan1));
    }

    [Fact]
    public void PickLatest_OnSameEffectiveInstant_PrefersTheMostRecentlyCreatedRow()
    {
        // A correction carries the same effective instant but a later created
        // stamp, so it must supersede the row it corrects.
        var original = Create(rate: 48m, effectiveFromUtc: Jan1);
        original.CreatedAtUtc = Jan1;

        var correction = Create(rate: 49m, effectiveFromUtc: Jan1);
        correction.CreatedAtUtc = Jan1.AddMinutes(5);

        var picked = ExchangeRate.PickLatest([correction, original], Jan1.AddDays(1));

        Assert.Same(correction, picked);
    }

    private static ExchangeRate Create(
        string fromCurrency = "usd",
        string toCurrency = "egp",
        decimal rate = 48.5m,
        DateTimeOffset? effectiveFromUtc = null)
        => ExchangeRate.Create(
            fromCurrency,
            toCurrency,
            rate,
            effectiveFromUtc ?? Jan1).Value;
}