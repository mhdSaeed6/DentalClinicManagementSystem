using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Commands.UpdatePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.UpdatePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdatePatientCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingPatient_ShouldUpdatePatientDetails()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("OldName", "OldLast", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        var updatedContact = ContactInfo.Create("+963922222222", null, true, null).Value;
        var command = new UpdatePatientCommand(
            PatientId: patient.Id,
            FirstName: "NewName",
            LastName: "NewLast",
            ContactInfo: updatedContact,
            DateOfBirth: DateTime.UtcNow.AddYears(-25),
            Gender: Gender.Female,
            MedicalHistory: new MedicalHistory());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NewName", result.Value.FirstName);
        Assert.Equal("NewLast", result.Value.LastName);

        // استخدم AsNoTracking لتجبر EF Core على القراءة المباشرة من قاعدة البيانات
        var dbPatient = await _context.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == patient.Id);

        Assert.NotNull(dbPatient);
        Assert.Equal("NewName", dbPatient.FirstName);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatientId_ShouldReturnNotFound()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var command = new UpdatePatientCommand(
            PatientId: Guid.NewGuid(),
            FirstName: "Saeed",
            LastName: "Mhd",
            ContactInfo: contactInfo,
            DateOfBirth: DateTime.UtcNow.AddYears(-20),
            Gender: Gender.Male,
            MedicalHistory: new MedicalHistory());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}