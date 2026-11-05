using DentalClinic.Application.Features.Clinics.Queries.GetExchangeRate;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class GetExchangeRateQueryTests
{
    [Fact]
    public void CacheKey_ShouldDifferPerInstant()
    {
        // A historical invoice must not be served the rate that happens to be
        // current, so the instant is part of the key.
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        var then = now.AddMonths(-6);

        var current = new GetExchangeRateQuery();
        var atNow = new GetExchangeRateQuery(now);
        var atThen = new GetExchangeRateQuery(then);

        Assert.NotEqual(current.CacheKey, atNow.CacheKey);
        Assert.NotEqual(atNow.CacheKey, atThen.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldBeStable_ForTheSameInstant()
    {
        var instant = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        var first = new GetExchangeRateQuery(instant);
        var second = new GetExchangeRateQuery(instant);

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void Tags_ShouldCarryRateAndSettingsTags()
    {
        // Appending a rate busts the first tag; changing the currency pair
        // busts the second, so a cached answer can never describe a pair the
        // clinic no longer uses.
        var query = new GetExchangeRateQuery();

        Assert.Contains("exchange-rate", query.Tags);
        Assert.Contains("clinic-setting", query.Tags);
    }
}