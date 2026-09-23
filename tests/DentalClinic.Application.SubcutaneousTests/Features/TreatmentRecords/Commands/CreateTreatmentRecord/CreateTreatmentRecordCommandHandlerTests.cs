using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Commands.CreateTreatmentRecord;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateTreatmentRecordCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTreatmentRecordAndSaveToDb()
    {
        // Arrange
        var (patient, doctor) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentRecordCommand(
            patient.Id,
            doctor.Id,
            null,
            11,
            "Root Canal Treatment for Tooth 11",
            150);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(11, result.Value.ToothNumber);

        var dbRecord = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == result.Value.Id);

        Assert.NotNull(dbRecord);
        Assert.Equal(patient.Id, dbRecord.PatientId);
        Assert.Equal(doctor.Id, dbRecord.DoctorId);
        Assert.Equal(150, dbRecord.Cost);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatient_ShouldReturnPatientNotFound()
    {
        // Arrange
        var (_, doctor) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentRecordCommand(
            Guid.NewGuid(),
            doctor.Id,
            null,
            21,
            "Dental Filling",
            80);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctor_ShouldReturnDoctorNotFound()
    {
        // Arrange
        var (patient, _) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentRecordCommand(
            patient.Id,
            Guid.NewGuid(),
            null,
            21,
            "Dental Filling",
            80);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientAndDoctorAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        return (patient, doctor);
    }
}