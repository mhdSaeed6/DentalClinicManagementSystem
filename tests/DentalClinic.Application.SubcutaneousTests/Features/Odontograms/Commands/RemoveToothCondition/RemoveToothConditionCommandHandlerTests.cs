using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;
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

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Commands.RemoveToothCondition;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveToothConditionCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_WithLiveRow_ShouldSoftDeleteIt()
    {
        // Arrange: a live condition row exists for tooth 26; removing it must
        // soft-delete the row (no hard delete), carrying the deleting user id.
        var patient = await SeedPatientAsync();
        var seededRow = PatientTooth.Create(patient.Id, 26, ToothCondition.Caries, ToothSurface.Occlusal, "Occlusal caries observed.").Value;
        await _context.PatientTeeth.AddAsync(seededRow);
        await _context.SaveChangesAsync(default);

        var command = new RemoveToothConditionCommand(patient.Id, 26, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(command);

        // Assert: the round trip — the row is gone from the live set but still in
        // the table with the delete markers set.
        Assert.True(result.IsSuccess);

        var dbRow = await _context.PatientTeeth
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(t => t.PatientId == patient.Id && t.ToothNumber == 26);

        Assert.True(dbRow.IsDeleted);
        Assert.NotNull(dbRow.DeletedAtUtc);
        Assert.Equal(factory.CurrentUser.Id, dbRow.DeletedBy);

        var liveRows = await _context.PatientTeeth
            .AsNoTracking()
            .Where(t => t.PatientId == patient.Id && t.ToothNumber == 26)
            .ToListAsync();

        Assert.Empty(liveRows);
    }

    [Fact]
    public async Task Handle_WithoutLiveRow_ShouldSucceedAsNoOp()
    {
        // Arrange: never set — the removal is idempotent: clearing a tooth with
        // no condition row (or already cleared) succeeds without error.
        var patient = await SeedPatientAsync();

        var first = await _mediator.Send(new RemoveToothConditionCommand(patient.Id, 26, PatientAccessScope.None));
        var second = await _mediator.Send(new RemoveToothConditionCommand(patient.Id, 26, PatientAccessScope.None));

        // Assert
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var dbRows = await _context.PatientTeeth
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.PatientId == patient.Id)
            .ToListAsync();

        Assert.Empty(dbRows);
    }

    [Fact]
    public async Task Handle_AsReceptionist_ShouldReturnClinicalWriteForbidden()
    {
        // Arrange: receptionists may not clear clinical tooth state — the write
        // capability gate fires before any access check (treatment-record precedence).
        var patient = await SeedPatientAsync();
        factory.CurrentUser.AsReceptionist();

        var command = new RemoveToothConditionCommand(patient.Id, 26, PatientAccessScope.None);

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
        // denies the clear on another doctor's patient (fail-closed).
        var (patient, _) = await SeedPatientWithDoctorAppointmentAsync();

        var otherDoctorScope = new PatientAccessScope(IsRestricted: true, DoctorId: Guid.NewGuid(), UserId: null);
        var command = new RemoveToothConditionCommand(patient.Id, 26, otherDoctorScope);

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