namespace DentalClinic.Domain.Invoices;

/// <summary>
/// One row per numbering year, holding the last number handed out. This is the
/// serialisation point for issuance: the row is read with an update lock inside
/// the issuing transaction, so two concurrent issues can never share a number.
/// </summary>
/// <remarks>
/// Deliberately not an <c>AuditableEntity</c>: it is bookkeeping, not clinic
/// data, so it has no soft-delete filter, no audit columns and no concurrency
/// token. Its only invariant is <c>LastValue &gt;= 0</c>.
/// </remarks>
public class InvoiceNumberCounter
{
    /// <summary>Clinic-local calendar year the counter belongs to.</summary>
    public int Year { get; private set; }

    /// <summary>Highest number already handed out in <see cref="Year"/>.</summary>
    public int LastValue { get; private set; }

    private InvoiceNumberCounter() { }

    public InvoiceNumberCounter(int year, int lastValue)
    {
        if (lastValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastValue), lastValue, "Counter value cannot be negative.");
        }

        Year = year;
        LastValue = lastValue;
    }

    /// <summary>
    /// Reserves the next number and returns it. Persisted together with the
    /// invoice it belongs to, so a rolled-back issue releases the number instead
    /// of burning it.
    /// </summary>
    public int Increment()
    {
        LastValue++;
        return LastValue;
    }
}