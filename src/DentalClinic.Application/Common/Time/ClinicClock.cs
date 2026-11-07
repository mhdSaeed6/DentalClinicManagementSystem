using DentalClinic.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Common.Time;

/// <summary>
/// The clinic's calendar, not the server's. Every "today", "this month" and
/// report window is expressed in the timezone the clinic configured, so an
/// appointment at 23:30 in Cairo belongs to that day even though UTC has
/// already rolled over (AUDIT.md D-09).
/// </summary>
public static class ClinicClock
{
    /// <summary>
    /// Resolves the clinic timezone from the persisted profile. Falls back to
    /// the host timezone when the profile is missing or names a zone the host
    /// does not know — a typo must not break reporting.
    /// </summary>
    public static async Task<TimeZoneInfo> ResolveTimeZoneAsync(
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        var timeZoneId = await context.ClinicSettings
            .AsNoTracking()
            .Select(setting => setting.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        return Resolve(timeZoneId);
    }

    /// <summary>Resolves a timezone id, falling back to the host timezone.</summary>
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Local;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    /// <summary>The clinic-local calendar date of an instant.</summary>
    public static DateOnly LocalDate(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);

    /// <summary>
    /// The UTC window covering the inclusive local date range
    /// [<paramref name="from"/>, <paramref name="to"/>]. The end is exclusive,
    /// which is what keeps a 23:59 appointment in the range and a 00:00 one on
    /// the next day out of it.
    /// </summary>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtcExclusive) UtcWindow(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone) =>
        (StartOfLocalDay(from, timeZone), StartOfLocalDay(to.AddDays(1), timeZone));

    /// <summary>
    /// The first instant of a local calendar day, expressed in UTC.
    /// </summary>
    /// <remarks>
    /// Two daylight-saving edges are handled explicitly instead of throwing:
    /// a day whose midnight is skipped (spring forward) starts at the first
    /// valid instant, and a midnight that happens twice (autumn back) resolves
    /// to its first occurrence so the window never overlaps the previous day.
    /// </remarks>
    public static DateTimeOffset StartOfLocalDay(DateOnly date, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        var offset = timeZone.IsAmbiguousTime(local)
            ? timeZone.GetAmbiguousTimeOffsets(local).Max()
            : timeZone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}