using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Odontograms;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Commands.UpsertToothCondition;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpsertToothConditionCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_ForToothWithoutLiveRow_ShouldCreateTheRow()
    {
        // Arrange: a fresh patient with no tooth rows — the upsert is the create leg.
        var patient = await SeedPatientAsync();
        var command = new UpsertToothConditionCommand(
            patient.Id, 26, ToothCondition.Caries, ToothSurface.Occlusal, "Occlusal caries observed.", PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(command);

        // Assert: the returned summary matches the command and the row is
        // persisted exactly once.
        Assert.True(result.IsSuccess);
        Assert.Equal(26, result.Value.ToothNumber);
        Assert.Equal(ToothCondition.Caries, result.Value.Condition);
        Assert.Equal(ToothSurface.Occlusal, result.Value.Surface);
        Assert.Equal("Occlusal caries observed.", result.Value.Notes);
        Assert.NotNull(result.Value.LastModifiedUtc);

        var dbRows = await _context.PatientTeeth
            .AsNoTracking()
            .Where(t => t.PatientId == patient.Id && t.ToothNumber == 26)
            .ToListAsync();

        var dbRow = Assert.Single(dbRows);
        Assert.Equal(ToothCondition.Caries, dbRow.Condition);
        Assert.Equal(patient.Id, dbRow.PatientId);
    }

    [Fact]
    public async Task Handle_ForToothWithLiveRow_ShouldUpdateItWithoutDuplicating()
    {
        // Arrange: tooth 26 already carries a condition; the upsert is the update
        // leg and must not insert a second row for the same tooth.
        var patient = await SeedPatientAsync();
        var seededRow = PatientTooth.Create(patient.Id, 26, ToothCondition.Caries, ToothSurface.Occlusal, "Occlusal caries observed.").Value;
        await _context.PatientTeeth.AddAsync(seededRow);
        await _context.SaveChangesAsync(default);

        var command = new UpsertToothConditionCommand(
            patient.Id, 26, ToothCondition.Filled, ToothSurface.Distal, "Restored with composite.", PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ToothCondition.Filled, result.Value.Condition);
        Assert.Equal(ToothSurface.Distal, result.Value.Surface);
        Assert.Equal("Restored with composite.", result.Value.Notes);

        var dbRows = await _context.PatientTeeth
            .AsNoTracking()
            .Where(t => t.PatientId == patient.Id && t.ToothNumber == 26)
            .ToListAsync();

        var dbRow = Assert.Single(dbRows);
        Assert.Equal(ToothCondition.Filled, dbRow.Condition);
        Assert.Equal(ToothSurface.Distal, dbRow.Surface);
    }

    [Fact]
    public async Task Handle_AsReceptionist_ShouldReturnClinicalWriteForbidden()
    {
        // Arrange: receptionists may not write clinical tooth state — the write
        // capability gate fires before any access check (treatment-record precedence).
        var patient = await SeedPatientAsync();
        factory.CurrentUser.AsReceptionist();

        var command = new UpsertToothConditionCommand(
            patient.Id, 26, ToothCondition.Caries, null, null, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.ClinicalWriteForbidden.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorOutsidePatientScope_ShouldReturnPatientAccessForbidden()
    {
        // Arrange: the patient is linked to a doctor through an appointment, but
        // the restricted scope carries a different doctor's id — the scope check
        // denies the write on another doctor's patient (fail-closed).
        var (patient, _) = await SeedPatientWithDoctorAppointmentAsync();

        var otherDoctorScope = new PatientAccessScope(IsRestricted: true, DoctorId: Guid.NewGuid(), UserId: null);
        var command = new UpsertToothConditionCommand(
            patient.Id, 26, ToothCondition.Caries, null, null, otherDoctorScope);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.PatientAccessForbidden.Code, result.TopError.Code);
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var contactInfo = ContactInfo.Create("+963933333333", null, true, null).Value;
        var patient = Patient.Create("Layla", "Haddad", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        return patient;
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientWithDoctorAppointmentAsync()
    {
        var patientContactInfo = ContactInfo.Create("+963944444444", null, true, null).Value;
        var patient = Patient.Create("Maha", "Idris", patientContactInfo, DateTime.UtcNow.AddYears(-28), Gender.Female).Value;
        var doctorContactInfo = ContactInfo.Create("+963966666666", null, true, null).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", doctorContactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return (patient, doctor);
    }
}