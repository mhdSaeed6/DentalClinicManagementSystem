using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateTreatmentRecordCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateTreatmentRecordAndSaveToDb()
    {
        // Arrange
        var (record, patient, doctor) = await SeedRecordAsync();
        var command = new UpdateTreatmentRecordCommand(
            record.Id,
            patient.Id,
            doctor.Id,
            null,
            12,
            "Updated Procedure Details",
            200);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(12, result.Value.ToothNumber);

        var dbRecord = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == record.Id);

        Assert.NotNull(dbRecord);
        Assert.Equal(12, dbRecord.ToothNumber);
        Assert.Equal("Updated Procedure Details", dbRecord.ProcedureDetails);
        Assert.Equal(200, dbRecord.Cost);
    }

    [Fact]
    public async Task Handle_WithNonExistingRecordId_ShouldReturnTreatmentRecordNotFound()
    {
        // Arrange
        var (_, patient, doctor) = await SeedRecordAsync();
        var command = new UpdateTreatmentRecordCommand(
            Guid.NewGuid(),
            patient.Id,
            doctor.Id,
            null,
            11,
            "Procedure",
            100);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<(TreatmentRecord Record, Patient Patient, Doctor Doctor)> SeedRecordAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Gender.Male).Value;
        var record = TreatmentRecord.Create(patient.Id, doctor.Id, 11, "Initial Procedure", 100, null).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentRecords.AddAsync(record);
        await _context.SaveChangesAsync(default);

        return (record, patient, doctor);
    }
}