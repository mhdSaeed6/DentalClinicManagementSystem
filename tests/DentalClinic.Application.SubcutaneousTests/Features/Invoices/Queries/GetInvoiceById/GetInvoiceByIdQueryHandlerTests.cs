using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Queries.GetInvoiceById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoiceByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingInvoice_ShouldReturnInvoiceDtoWithPayments()
    {
        // Arrange
        var invoice = await SeedInvoiceAsync();
        var query = new GetInvoiceByIdQuery(invoice.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(invoice.Id, result.Value.Id);
        Assert.Equal(300, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Handle_WithNonExistingInvoiceId_ShouldReturnInvoiceNotFound()
    {
        // Arrange
        var query = new GetInvoiceByIdQuery(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<Invoice> SeedInvoiceAsync()
    {
        var contactinfo = ContactInfo.Create("1234567890").Value;
        var patient = Patient.Create("John", "Doe", contactinfo, new DateTime(1990, 1, 1), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);

        var invoice = Invoice.Create(patient.Id, 300, null).Value;
        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync(default);

        return invoice;
    }
}