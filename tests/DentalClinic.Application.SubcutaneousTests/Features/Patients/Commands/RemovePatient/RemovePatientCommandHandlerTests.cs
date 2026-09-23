using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Commands.RemovePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.RemovePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemovePatientCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingPatient_ShouldSoftDeletePatient()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("To Delete", "Patient", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        var command = new RemovePatientCommand(patient.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbPatient = await _context.Patients
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == patient.Id);

        Assert.NotNull(dbPatient);
        Assert.True(dbPatient.IsDeleted);
    }
}