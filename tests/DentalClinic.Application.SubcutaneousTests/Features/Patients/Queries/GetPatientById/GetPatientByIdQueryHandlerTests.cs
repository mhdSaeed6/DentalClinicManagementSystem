using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Queries.GetPatientById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Queries.GetPatientById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetPatientByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingPatientId_ShouldReturnPatientDto()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Saeed", "Mhd", contactInfo, DateTime.UtcNow.AddYears(-20), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        var query = new GetPatientByIdQuery(patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(patient.Id, result.Value.Id);
        Assert.Equal("Saeed", result.Value.FirstName);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatientId_ShouldReturnNotFound()
    {
        var query = new GetPatientByIdQuery(Guid.NewGuid());
        var result = await _mediator.Send(query);
        Assert.True(result.IsError);
    }
}