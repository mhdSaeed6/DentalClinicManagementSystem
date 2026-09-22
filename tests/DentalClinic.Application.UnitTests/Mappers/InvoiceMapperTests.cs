using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Invoices;
using DentalClinic.Tests.Common.Invoices;

using Xunit;

namespace DentalClinic.Application.UnitTests.Mappers;

public class InvoiceMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var invoice = InvoiceFactory.CreateInvoice(patientId).Value;

        // Act
        var dto = invoice.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(invoice.Id, dto.Id);
        Assert.Equal(invoice.PatientId, dto.PatientId);
        Assert.Equal(invoice.AppointmentId, dto.AppointmentId);
        Assert.Equal(invoice.TotalAmount, dto.TotalAmount);
        Assert.Equal(invoice.PaidAmount, dto.PaidAmount);
        Assert.Equal(invoice.RemainingAmount, dto.RemainingAmount);
        Assert.Equal(invoice.Status, dto.Status);
        Assert.NotNull(dto.Payments);
        Assert.Equal(invoice.Payments.Count, dto.Payments.Count);
    }

    [Fact]
    public void ToDto_WhenInvoiceIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Invoice invoice = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => invoice.ToDto());
    }

    [Fact]
    public void PaymentToDto_ShouldMapCorrectly()
    {
        var invoiceId = Guid.NewGuid();
        var payment1 = Payment.Create(invoiceId, 100.0m, "Cache");

        Assert.True(payment1.IsSuccess);
        var payment = payment1.Value;

        // Act
        var dto = payment.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(payment.Id, dto.Id);
        Assert.Equal(payment.InvoiceId, dto.InvoiceId);
        Assert.Equal(payment.Amount, dto.Amount);
        Assert.Equal(payment.PaidAtUtc, dto.PaidAtUtc);
        Assert.Equal(payment.TransactionNotes, dto.TransactionNotes);
    }

    [Fact]
    public void PaymentToDto_WhenPaymentIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Payment payment = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => payment.ToDto());
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var invoice1 = InvoiceFactory.CreateInvoice(guid).Value;
        var invoice2 = InvoiceFactory.CreateInvoice(guid).Value;
        var invoices = new List<Invoice> { invoice1, invoice2 };

        // Act
        var dtos = invoices.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(invoice1.Id, dtos[0].Id);
        Assert.Equal(invoice2.Id, dtos[1].Id);
    }

    [Fact]
    public void ToDtos_WhenListIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var invoices = new List<Invoice>();

        // Act
        var dtos = invoices.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
}