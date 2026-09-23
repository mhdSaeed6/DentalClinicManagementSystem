using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.AddInvoicePayment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AddInvoicePaymentCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidPaymentAmount_ShouldRegisterPaymentAndSaveToDb()
    {
        // Arrange
        var invoice = await SeedInvoiceAsync(1000);
        var command = new AddInvoicePaymentCommand(invoice.Id, 400, "First installment");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        var dbPayment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InvoiceId == invoice.Id);

        Assert.NotNull(dbInvoice);
        Assert.Equal(400, dbInvoice.PaidAmount);

        Assert.NotNull(dbPayment);
        Assert.Equal(400, dbPayment.Amount);
        Assert.Equal("First installment", dbPayment.TransactionNotes);
    }

    [Fact]
    public async Task Handle_WithNonExistingInvoice_ShouldReturnInvoiceNotFound()
    {
        // Arrange
        var command = new AddInvoicePaymentCommand(Guid.NewGuid(), 100, "Test Payment");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WhenPaymentAmountExceedsRemainingBalance_ShouldReturnError()
    {
        // Arrange
        var invoice = await SeedInvoiceAsync(500);
        var command = new AddInvoicePaymentCommand(invoice.Id, 600, "Overpaying"); // أكبر من قيمة الفاتورة

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<Invoice> SeedInvoiceAsync(decimal totalAmount)
    {
        var contactinfo = ContactInfo.Create("1234567890").Value;
        var patient = Patient.Create("John", "Doe", contactinfo, new DateTime(1990, 1, 1), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);

        var invoice = Invoice.Create(patient.Id, totalAmount, null).Value;
        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync(default);

        return invoice;
    }
}