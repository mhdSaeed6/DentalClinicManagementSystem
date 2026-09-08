using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Invoices;

public class Payment : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string? TransactionNotes { get; private set; }

    internal Payment(Guid id, Guid invoiceId, decimal amount, string? transactionNotes)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        PaidAtUtc = DateTime.UtcNow;
        TransactionNotes = transactionNotes;
    }

    private Payment() { }
}