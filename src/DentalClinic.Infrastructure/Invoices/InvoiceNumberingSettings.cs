namespace DentalClinic.Infrastructure.Invoices;

/// <summary>
/// Numbering policy, bound from the <c>InvoiceNumbering</c> configuration
/// section. Read lazily through <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>
/// so a test host or a late configuration source is observed.
/// </summary>
public sealed class InvoiceNumberingSettings
{
    public const string SectionName = "InvoiceNumbering";

    /// <summary>Prefix before the year, e.g. <c>INV</c>.</summary>
    public string Prefix { get; init; } = "INV";

    /// <summary>Minimum digits for the yearly sequence.</summary>
    public int Padding { get; init; } = 4;

    /// <summary>
    /// IANA/Windows timezone id used to decide which calendar year a number
    /// belongs to. Blank means the host's local timezone, which is what the
    /// container's <c>TZ</c> environment variable sets.
    /// </summary>
    public string? TimeZoneId { get; init; }
}