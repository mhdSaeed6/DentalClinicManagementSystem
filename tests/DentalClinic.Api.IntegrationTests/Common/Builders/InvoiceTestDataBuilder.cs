using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums; // أو المكان الخاص بـ InvoiceStatus

namespace DentalClinic.Tests.Common.Builders;

public class InvoiceTestDataBuilder : ITestDataBuilder<Invoice>
{
    private Guid _patientId = Guid.NewGuid();
    private Guid? _appointmentId = null;
    private decimal _totalAmount = 150m;
    private PaymentStatus _status = PaymentStatus.Unpaid;

    public static InvoiceTestDataBuilder Create() => new();

    public InvoiceTestDataBuilder ForPatient(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public InvoiceTestDataBuilder ForAppointment(Guid appointmentId)
    {
        _appointmentId = appointmentId;
        return this;
    }

    public InvoiceTestDataBuilder WithAmount(decimal totalAmount, decimal discount = 0m)
    {
        _totalAmount = totalAmount;
        return this;
    }

    public InvoiceTestDataBuilder Paid()
    {
        _status = PaymentStatus.Paid;
        return this;
    }

    public Invoice Build()
    {
        var invoice = Invoice.Create(_patientId, _totalAmount, _appointmentId).Value;

        if (_status == PaymentStatus.Paid)
        {
            invoice.RegisterPayment(_totalAmount);
        }

        return invoice;
    }
}