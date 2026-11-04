namespace DentalClinic.Application.Common.Interfaces;

/// <summary>
/// Allocates the next human-readable invoice number (e.g. <c>INV-2026-0042</c>).
/// </summary>
/// <remarks>
/// Implementations must be called from inside the transaction that persists the
/// invoice, so the reserved number and the invoice commit (or roll back)
/// together. The numbering policy (prefix, padding, yearly reset, clinic
/// timezone) lives in Infrastructure; the domain only stores the resulting
/// string.
/// </remarks>
public interface IInvoiceNumberGenerator
{
    /// <summary>
    /// Reserves the next number for the current clinic-local year.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the caller forgot to open a transaction. Allocation without
    /// one would release the counter row's update lock immediately and let two
    /// concurrent issues read the same value.
    /// </exception>
    Task<string> NextAsync(CancellationToken cancellationToken);
}