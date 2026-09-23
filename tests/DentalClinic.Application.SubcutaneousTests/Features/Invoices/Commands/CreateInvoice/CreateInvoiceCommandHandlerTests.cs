using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.CreateInvoice;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateInvoiceCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateInvoiceAndSaveToDb()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var command = new CreateInvoiceCommand(patient.Id, 500, null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(500, result.Value.TotalAmount);

        var dbInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == result.Value.Id);

        Assert.NotNull(dbInvoice);
        Assert.Equal(patient.Id, dbInvoice.PatientId);
        Assert.Equal(500, dbInvoice.TotalAmount);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatient_ShouldReturnPatientNotFound()
    {
        // Arrange
        var command = new CreateInvoiceCommand(Guid.NewGuid(), 500, null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistingAppointment_ShouldReturnAppointmentNotFound()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var command = new CreateInvoiceCommand(patient.Id, 500, Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
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