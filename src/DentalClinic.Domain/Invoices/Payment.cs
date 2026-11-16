using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Domain.Invoices;

public class Payment : AuditableEntity
{
    /// <summary>
    /// أقصى طول لمرجع الدفعة (رقم العملية، آخر أربعة أرقام من البطاقة، رقم الحوالة).
    /// </summary>
    public const int MaxReferenceLength = 64;

    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string? TransactionNotes { get; private set; }

    /// <summary>
    /// طريقة الدفع. الافتراضي نقداً، وهو ما تحصل عليه الصفوف القديمة بعد الترحيل.
    /// </summary>
    public PaymentMethod Method { get; private set; } = PaymentMethod.Cash;

    /// <summary>
    /// مرجع اختياري للدفعة (رقم البطاقة/الحوالة). لا يدخل في أي حساب مالي.
    /// </summary>
    public string? Reference { get; private set; }

    private Payment() { }

    private Payment(
        Guid id,
        Guid invoiceId,
        decimal amount,
        string? transactionNotes,
        PaymentMethod method,
        string? reference)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        PaidAtUtc = DateTime.UtcNow;
        TransactionNotes = transactionNotes;
        Method = method;
        Reference = reference;
    }

    // 💡 Factory Method لإنشاء دفعة مستقلة
    public static Result<Payment> Create(
        Guid invoiceId,
        decimal amount,
        string? notes = null,
        PaymentMethod method = PaymentMethod.Cash,
        string? reference = null)
    {
        if (amount <= 0)
        {
            return InvoiceErrors.InvalidPaymentAmount;
        }

        // المرجع يُقصّ ويُفحص هنا أيضاً، لا في الـ Validator وحده، حتى يبقى
        // العمود nvarchar(64) آمناً مهما كان مصدر الإنشاء.
        var trimmedReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (trimmedReference is not null && trimmedReference.Length > MaxReferenceLength)
        {
            return InvoiceErrors.InvalidPaymentReference;
        }

        return new Payment(
            Guid.NewGuid(),
            invoiceId,
            amount,
            notes?.Trim(),
            method,
            trimmedReference);
    }
}