using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Tests.Common.Invoices;

namespace DentalClinic.Domain.UnitTests.Invoices;

public class InvoiceTest
{
    [Fact]
    public void CreateInvoice_ShouldReturnInvoice_WhenValidParameters()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var totalAmount = 100.0m;

        // Act
        var result = InvoiceFactory.CreateInvoice(patientId, totalAmount);

        // Assert
        Assert.True(result.IsSuccess);

        var patient = result.Value;

        Assert.NotNull(patient);
        Assert.Equal(patientId, patient.PatientId);
        Assert.Equal(totalAmount, patient.TotalAmount);
        Assert.Equal(0, patient.PaidAmount);
        Assert.Equal(PaymentStatus.Unpaid, patient.Status);
    }

    [Fact]
    public void CreateInvoice_ShouldReturnError_WhenTotalAmountIsZeroOrNegative()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var totalAmount = 0.0m;

        // Act
        var result = InvoiceFactory.CreateInvoice(patientId, totalAmount);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidTotalAmount.Code, result.TopError.Code);
    }

    [Fact]
    public void RegisterPayment_ShouldReject_AmountExceedingRemainingBalance()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice(Guid.NewGuid(), totalAmount: 100.0m).Value;

        // Act
        var result = invoice.RegisterPayment(150.0m);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaymentExceedsRemainingAmount.Code, result.TopError.Code);
        Assert.Equal(0.0m, invoice.PaidAmount);
    }

    [Fact]
    public void RegisterPayment_OnStaleCopy_ShouldNotDoubleCount_WithinOneInstance()
    {
        // Reproduces the concurrency scenario at the domain level: a second
        // payment attempt against an invoice whose balance is already exhausted
        // must be refused. Together with the Invoice.RowVersion concurrency token
        // and the SERIALIZABLE transaction in AddInvoicePaymentCommandHandler,
        // this prevents PaidAmount being advanced twice.
        var invoice = InvoiceFactory.CreateInvoice(Guid.NewGuid(), totalAmount: 100.0m).Value;

        var first = invoice.RegisterPayment(60.0m);
        var second = invoice.RegisterPayment(60.0m);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsError);
        Assert.Equal(60.0m, invoice.PaidAmount);
        Assert.Equal(PaymentStatus.PartiallyPaid, invoice.Status);
    }

    [Fact]
    public void RegisterPayment_ShouldReject_PaymentOnDraftInvoice()
    {
        // Arrange — Invoice.Create defaults to Issued; a Draft must be requested
        // explicitly, and drafts are never payable.
        var invoice = Invoice.Create(
            Guid.NewGuid(),
            totalAmount: 100.0m,
            lifecycleStatus: InvoiceLifecycleStatus.Draft).Value;

        // Act
        var result = invoice.RegisterPayment(10.0m);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotPayDraft.Code, result.TopError.Code);
        Assert.Equal(0.0m, invoice.PaidAmount);
    }
}