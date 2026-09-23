using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Commands.CreatePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.CreatePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreatePatientCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePatientAndSaveToDb()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var medicalHistory = new MedicalHistory();

        var command = new CreatePatientCommand(
            FirstName: "Saeed",
            LastName: "Mhd",
            ContactInfo: contactInfo,
            DateOfBirth: DateTime.UtcNow.AddYears(-20),
            Gender: Gender.Male,
            MedicalHistory: medicalHistory);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Saeed", result.Value.FirstName);
        Assert.Equal("Mhd", result.Value.LastName);

        var dbPatient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == result.Value.Id);
        Assert.NotNull(dbPatient);
        Assert.Equal("Saeed", dbPatient.FirstName);
    }
}