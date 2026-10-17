using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Invoices;

public class InvoiceLine : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Amount { get; private set; }

    private InvoiceLine() { }

    private InvoiceLine(Guid id, Guid invoiceId, string description, decimal amount)
        : base(id)
    {
        InvoiceId = invoiceId;
        Description = description;
        Amount = amount;
    }

    internal static InvoiceLine Create(string description, decimal amount)
    {
        return new InvoiceLine(Guid.NewGuid(), Guid.Empty, description, amount);
    }

    internal void SetInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }

    internal void Update(string description, decimal amount)
    {
        Description = description;
        Amount = amount;
    }
}