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

    public static readonly Error NoBillableItems = Error.Validation(
        "Invoice.NoBillableItems",
        "This appointment has no billable service or treatments to invoice.");

    public static readonly Error ExistingInvoiceForAppointment = Error.Conflict(
        "Invoice.ExistingForAppointment",
        "An invoice has already been generated for this appointment. Review or pay the existing invoice instead.");

    public static readonly Error AppointmentNotCompleted = Error.Conflict(
        "Invoice.AppointmentNotCompleted",
        "The invoice can only be generated from a completed appointment.");

    public static readonly Error InvalidLineAmount = Error.Validation(
        "Invoice.InvalidLineAmount",
        "Invoice line amount must be greater than zero.");

    public static readonly Error InvoiceLineNotFound = Error.NotFound(
        "Invoice.InvoiceLineNotFound",
        "Invoice line not found.");

    public static readonly Error CannotModifyIssuedInvoice = Error.Conflict(
        "Invoice.CannotModifyIssuedInvoice",
        "Cannot modify an issued or locked invoice.");

    public static readonly Error CannotPayDraft = Error.Validation(
        "Invoice.CannotPayDraft",
        "Draft invoices cannot accept payments. Issue the invoice first.");

    public static readonly Error InvalidState = Error.Validation(
        "Invoice.InvalidState",
        "The invoice is in an invalid state for the requested operation.");

    public static readonly Error ManualAdjustmentsWouldBeLost = Error.Conflict(
        "Invoice.ManualAdjustmentsWouldBeLost",
        "This draft has manual adjustments. Confirm to discard them and recalculate from the appointment.");

    public static readonly Error DraftHasNoSourceAppointment = Error.Validation(
        "Invoice.DraftHasNoSourceAppointment",
        "This invoice was not created from an appointment, so it cannot be refreshed from one.");

    public static readonly Error AppointmentPatientMismatch = Error.Validation(
        "Invoice.AppointmentPatientMismatch",
        "The selected appointment does not belong to the selected patient.");

    /// <summary>
    /// Returned when another request modified the invoice first. The caller
    /// should retry against the refreshed invoice rather than assume success.
    /// </summary>
    public static readonly Error ConcurrentModification = Error.Conflict(
        "Invoice.ConcurrentModification",
        "The invoice was modified by another request. Reload it and try again.");
}