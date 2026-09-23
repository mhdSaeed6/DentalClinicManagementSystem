using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoices;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Queries.GetInvoices;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoicesQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithFiltersAndPagination_ShouldReturnFilteredPaginatedList()
    {
        // Arrange
        var patient = await SeedPatientAsync();

        var invoice1 = Invoice.Create(patient.Id, 200, null).Value;
        var invoice2 = Invoice.Create(patient.Id, 400, null).Value;

        // فاتورة مدفوعة بالكامل للتأكد من الفلترة بالحالة
        var invoice3 = Invoice.Create(patient.Id, 150, null).Value;
        invoice3.RegisterPayment(150);

        await _context.Invoices.AddRangeAsync(invoice1, invoice2, invoice3);
        await _context.SaveChangesAsync(default);

        var query = new GetInvoicesQuery(
            PageNumber: 1,
            PageSize: 10,
            PatientId: patient.Id,
            Status: PaymentStatus.Unpaid);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items!.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(PaymentStatus.Unpaid, item.Status));
    }

    [Fact]
    public async Task Handle_WhenNoMatchingInvoices_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new GetInvoicesQuery(PageNumber: 1, PageSize: 10, PatientId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var contactinfo = ContactInfo.Create("1234567890").Value;
        var patient = Patient.Create("John", "Doe", contactinfo, new DateTime(1990, 1, 1), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        return patient;
    }
}