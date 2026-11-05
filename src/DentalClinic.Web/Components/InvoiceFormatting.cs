using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

/// <summary>
/// Presentation rules for invoice identity, kept pure so they can be tested
/// without rendering a component.
/// </summary>
public static class InvoiceFormatting
{
    /// <summary>
    /// The bill's identity for display: its number once issued, or a short id
    /// while it is still an unnumbered draft.
    /// </summary>
    /// <remarks>
    /// A draft has no number by design (numbers are allocated at issuance), so
    /// this must never invent one: showing a plausible-looking number on a
    /// document that is not a bill yet would be misleading.
    /// </remarks>
    public static string NumberLabel(InvoiceDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
            ? $"#{invoice.Id.ToString()[..8]}"
            : invoice.InvoiceNumber.Trim();
    }

    /// <summary>
    /// Only an issued invoice is a real bill, so only it may be printed as one.
    /// </summary>
    public static bool IsPrintable(InvoiceDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return invoice.LifecycleStatus == InvoiceLifecycleStatus.Issued;
    }
}