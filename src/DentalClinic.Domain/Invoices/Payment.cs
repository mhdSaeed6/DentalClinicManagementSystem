using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Invoices;

public class Payment : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string? TransactionNotes { get; private set; }

    private Payment() { }

    private Payment(Guid id, Guid invoiceId, decimal amount, string? transactionNotes)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        PaidAtUtc = DateTime.UtcNow;
        TransactionNotes = transactionNotes;
    }

    // 💡 Factory Method لإنشاء دفعة مستقلة
    public static Result<Payment> Create(Guid invoiceId, decimal amount, string? notes = null)
    {
        if (amount <= 0)
        {
            return InvoiceErrors.InvalidPaymentAmount;
        }

        return new Payment(
            Guid.NewGuid(),
            invoiceId,
            amount,
            notes?.Trim());
    }
}