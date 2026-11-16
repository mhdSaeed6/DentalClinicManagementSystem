namespace DentalClinic.Domain.Invoices.Enums;

/// <summary>
/// كيف دُفعت الفاتورة. تُخزَّن كسلسلة نصية، والقيمة الافتراضية نقداً حتى تبقى
/// الدفعات القديمة (التي لم تُسجَّل طريقتها) صحيحة.
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Other = 4
}