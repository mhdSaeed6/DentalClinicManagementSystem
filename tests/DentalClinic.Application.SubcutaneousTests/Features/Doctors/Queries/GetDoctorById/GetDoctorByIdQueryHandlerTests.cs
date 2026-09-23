using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Queries.GetDoctorById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDoctorByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingDoctorId_ShouldReturnDoctorDto()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var doctor = Doctor.Create("Saeed", "Mhd", "Orthodontics", contactInfo, Gender.Male).Value;

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        var query = new GetDoctorByIdQuery(doctor.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(doctor.Id, result.Value.Id);
        Assert.Equal("Saeed", result.Value.FirstName);
        Assert.Equal("Mhd", result.Value.LastName);
        Assert.Equal("Orthodontics", result.Value.Specialization);
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctorId_ShouldReturnDoctorNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var query = new GetDoctorByIdQuery(nonExistingId);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "Doctor.NotFound" || e.Description.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }
}