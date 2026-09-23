using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentRecordByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingRecord_ShouldReturnTreatmentRecordDto()
    {
        // Arrange
        var record = await SeedTreatmentRecordAsync();
        var query = new GetTreatmentRecordByIdQuery(record.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(record.Id, result.Value.Id);
        Assert.Equal(11, result.Value.ToothNumber);
        Assert.Equal(200, result.Value.Cost);
    }

    [Fact]
    public async Task Handle_WithNonExistingRecordId_ShouldReturnTreatmentRecordNotFound()
    {
        // Arrange
        var query = new GetTreatmentRecordByIdQuery(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<TreatmentRecord> SeedTreatmentRecordAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Gender.Male).Value;

        var record = TreatmentRecord.Create(
            patient.Id,
            doctor.Id,
            11,
            "Root Canal Treatment",
            200,
            null).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentRecords.AddAsync(record);
        await _context.SaveChangesAsync(default);

        return record;
    }
}