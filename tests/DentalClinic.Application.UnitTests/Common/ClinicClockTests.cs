using DentalClinic.Application.Common.Time;

using Xunit;

namespace DentalClinic.Application.UnitTests.Common;

/// <summary>
/// The clinic calendar is the whole point of <see cref="ClinicClock"/>: a report
/// window must follow the clinic's day, not the server's. These tests pin the
/// boundaries down, including the two daylight-saving edges.
/// </summary>
public class ClinicClockTests
{
    [Fact]
    public void StartOfLocalDay_ReturnsTheUtcInstantOfLocalMidnight()
    {
        var result = ClinicClock.StartOfLocalDay(new DateOnly(2026, 11, 5), PlusTwo());

        Assert.Equal(new DateTimeOffset(2026, 11, 4, 22, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void UtcWindow_CoversTheWholeInclusiveLocalDay()
    {
        // A 23:59 appointment belongs to the day; the next day's 00:00 does not.
        var (start, endExclusive) = ClinicClock.UtcWindow(
            new DateOnly(2026, 11, 5),
            new DateOnly(2026, 11, 5),
            PlusTwo());

        Assert.Equal(new DateTimeOffset(2026, 11, 4, 22, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 22, 0, 0, TimeSpan.Zero), endExclusive);
        Assert.Equal(TimeSpan.FromDays(1), endExclusive - start);
    }

    [Fact]
    public void UtcWindow_MultiDayRange_StartsAtTheFirstDayAndEndsAfterTheLast()
    {
        var (start, endExclusive) = ClinicClock.UtcWindow(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 5),
            PlusTwo());

        Assert.Equal(new DateTimeOffset(2026, 10, 31, 22, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 22, 0, 0, TimeSpan.Zero), endExclusive);
    }

    [Fact]
    public void LocalDate_UsesTheClinicZone_NotUtc()
    {
        // 23:30 UTC is already "tomorrow" in a +02:00 clinic.
        var instant = new DateTimeOffset(2026, 11, 4, 23, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 5), ClinicClock.LocalDate(instant, PlusTwo()));
    }

    [Fact]
    public void StartOfLocalDay_WhenMidnightIsSkipped_ReturnsTheFirstValidInstant()
    {
        // Spring forward: 00:00 does not exist on this day. The window must
        // still start at the first moment that does, not throw.
        var result = ClinicClock.StartOfLocalDay(new DateOnly(2026, 3, 29), DaylightZone());

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 22, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void StartOfLocalDay_WhenMidnightIsRepeated_ResolvesToItsFirstOccurrence()
    {
        // Autumn back: 00:00 happens twice. Taking the first occurrence keeps
        // the window from overlapping the previous day.
        var result = ClinicClock.StartOfLocalDay(new DateOnly(2026, 10, 25), DaylightZone());

        Assert.Equal(new DateTimeOffset(2026, 10, 24, 21, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void UtcWindow_OnTheDayClocksFallBack_IsTwentyFiveHoursLong()
    {
        var (start, endExclusive) = ClinicClock.UtcWindow(
            new DateOnly(2026, 10, 25),
            new DateOnly(2026, 10, 25),
            DaylightZone());

        Assert.Equal(TimeSpan.FromHours(25), endExclusive - start);
    }

    [Fact]
    public void Resolve_WithNoConfiguredZone_FallsBackToTheHostZone()
    {
        Assert.Equal(TimeZoneInfo.Local, ClinicClock.Resolve(null));
        Assert.Equal(TimeZoneInfo.Local, ClinicClock.Resolve("   "));
    }

    [Fact]
    public void Resolve_WithAnUnknownZone_FallsBackInsteadOfThrowing()
    {
        // A typo in the clinic profile must not break every report.
        Assert.Equal(TimeZoneInfo.Local, ClinicClock.Resolve("Mars/Olympus_Mons"));
    }

    [Fact]
    public void Resolve_WithAKnownZone_ReturnsIt()
    {
        Assert.Equal(TimeZoneInfo.Utc, ClinicClock.Resolve("UTC"));
    }

    private static TimeZoneInfo PlusTwo() =>
        TimeZoneInfo.CreateCustomTimeZone("Test/PlusTwo", TimeSpan.FromHours(2), "Test +02", "Test +02");

    /// <summary>
    /// A portable zone with the two edges that matter: midnight skipped when
    /// daylight time starts, and midnight repeated when it ends.
    /// </summary>
    private static TimeZoneInfo DaylightZone()
    {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            dateStart: new DateTime(2020, 1, 1),
            dateEnd: new DateTime(2030, 12, 31),
            daylightDelta: TimeSpan.FromHours(1),
            daylightTransitionStart: TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 0, 0, 0),
                month: 3,
                week: 5,
                dayOfWeek: DayOfWeek.Sunday),
            daylightTransitionEnd: TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 1, 0, 0),
                month: 10,
                week: 5,
                dayOfWeek: DayOfWeek.Sunday));

        return TimeZoneInfo.CreateCustomTimeZone(
            "Test/Daylight",
            TimeSpan.FromHours(2),
            "Test Daylight",
            "Test Standard",
            "Test Daylight",
            [rule]);
    }
}