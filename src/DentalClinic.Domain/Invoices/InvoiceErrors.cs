using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Invoices;

public static class InvoiceErrors
{
    public static readonly Error InvalidTotalAmount = Error.Validation(
        "Invoice.InvalidTotalAmount",
        "Total amount must be greater than zero.");

    public static readonly Error InvalidPaymentAmount = Error.Validation(
        "Invoice.InvalidPaymentAmount",
        "Payment amount must be greater than zero.");

    public static readonly Error PaymentExceedsRemainingAmount = Error.Validation(
        "Invoice.PaymentExceedsRemainingAmount",
        "Payment amount exceeds the remaining balance of the invoice.");

    public static readonly Error AlreadyFullyPaid = Error.Validation(
        "Invoice.AlreadyFullyPaid",
        "Invoice is already fully paid.");
}