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
}