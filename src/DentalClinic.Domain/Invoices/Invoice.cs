using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Domain.Invoices;

public class Invoice : AuditableEntity
{
    private readonly List<Payment> _payments = [];

    public Guid PatientId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public PaymentStatus Status { get; private set; }

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Invoice() { }

    private Invoice(
        Guid id,
        Guid patientId,
        Guid? appointmentId,
        decimal totalAmount)
        : base(id)
    {
        PatientId = patientId;
        AppointmentId = appointmentId;
        TotalAmount = totalAmount;
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
    }

    public static Result<Invoice> Create(
        Guid patientId,
        decimal totalAmount,
        Guid? appointmentId = null)
    {
        if (totalAmount <= 0)
        {
            return InvoiceErrors.InvalidTotalAmount;
        }

        return new Invoice(
            Guid.NewGuid(),
            patientId,
            appointmentId,
            totalAmount);
    }

    public Result<Updated> RegisterPayment(decimal amount)
    {
        if (amount <= 0)
        {
            return InvoiceErrors.InvalidPaymentAmount;
        }

        if (Status == PaymentStatus.Paid)
        {
            return InvoiceErrors.AlreadyFullyPaid;
        }

        if (amount > RemainingAmount)
        {
            return InvoiceErrors.PaymentExceedsRemainingAmount;
        }

        // تحديث المبالغ والحالة فقط داخل الفاتورة
        PaidAmount += amount;
        Status = (PaidAmount == TotalAmount) ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

        return Result.Updated;
    }
}