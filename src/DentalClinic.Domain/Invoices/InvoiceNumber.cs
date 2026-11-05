using System.Globalization;

namespace DentalClinic.Domain.Invoices;

/// <summary>
/// Pure formatting rule for the human-readable invoice number. Kept in the
/// domain (and free of I/O) so the format can be pinned by fast unit tests
/// without a database or a clock.
/// </summary>
public static class InvoiceNumber
{
    public const string DefaultPrefix = "INV";
    public const int DefaultPadding = 4;

    /// <summary>
    /// Builds <c>{prefix}-{year}-{sequence}</c>, e.g. <c>INV-2026-0042</c>.
    /// </summary>
    /// <remarks>
    /// The sequence is zero-padded but never truncated: past 9999 it simply
    /// grows a digit, because silently wrapping would reissue an existing
    /// number.
    /// </remarks>
    public static string Format(string? prefix, int padding, int year, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(year);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);

        var normalizedPrefix = string.IsNullOrWhiteSpace(prefix) ? DefaultPrefix : prefix.Trim();
        var normalizedPadding = padding is >= 1 and <= 10 ? padding : DefaultPadding;

        var padded = sequence.ToString(
            "D" + normalizedPadding.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{normalizedPrefix}-{year:D4}-{padded}");
    }
}