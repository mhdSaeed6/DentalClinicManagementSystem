using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Odontograms;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Queries.GetPatientOdontogram;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetPatientOdontogramQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_ForFreshPatient_ShouldReturnEmptyTeethAndHistory()
    {
        // Arrange: every patient starts "unknown until touched" — no condition
        // rows and no history (plan decision 2: no backfill).
        var patient = await SeedPatientAsync();
        var query = new GetPatientOdontogramQuery(patient.Id, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(patient.Id, result.Value.PatientId);
        Assert.Empty(result.Value.Teeth);
        Assert.Empty(result.Value.History);
    }

    [Fact]
    public async Task Handle_WithTeethAndRecords_ShouldReturnSummariesGroupedPerTooth()
    {
        // Arrange: tooth 26 carries a live condition row plus two treatment
        // records; tooth 11 has history only — the "history but no state yet"
        // marker appears through the history while Teeth stays empty for it.
        var patient = await SeedPatientAsync();
        var contactInfo = ContactInfo.Create("+963955555555", null, true, null).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Gender.Male).Value;
        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        var cariesRow = PatientTooth.Create(patient.Id, 26, ToothCondition.Caries, ToothSurface.Occlusal, "Deep caries on the occlusal surface.").Value;
        var healthyRow = PatientTooth.Create(patient.Id, 21, ToothCondition.Healthy).Value;
        await _context.PatientTeeth.AddRangeAsync(cariesRow, healthyRow);

        var rootCanal = TreatmentRecord.Create(patient.Id, doctor.Id, 26, "Root Canal Treatment", 260, null).Value;
        var filling = TreatmentRecord.Create(patient.Id, doctor.Id, 26, "Composite restoration.", 180, null).Value;
        var polishing = TreatmentRecord.Create(patient.Id, doctor.Id, 11, "Scaling and polishing.", 120, null).Value;
        await _context.TreatmentRecords.AddRangeAsync(rootCanal, filling, polishing);
        await _context.SaveChangesAsync(default);

        var query = new GetPatientOdontogramQuery(patient.Id, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(query);

        // Assert: summaries are the live condition rows, ordered by tooth number.
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 21, 26 }, result.Value.Teeth.Select(t => t.ToothNumber));

        var cariesSummary = result.Value.Teeth.Single(t => t.ToothNumber == 26);
        Assert.Equal(ToothCondition.Caries, cariesSummary.Condition);
        Assert.Equal(ToothSurface.Occlusal, cariesSummary.Surface);
        Assert.Equal("Deep caries on the occlusal surface.", cariesSummary.Notes);
        Assert.NotNull(cariesSummary.LastModifiedUtc);

        var healthySummary = result.Value.Teeth.Single(t => t.ToothNumber == 21);
        Assert.Equal(ToothCondition.Healthy, healthySummary.Condition);
        Assert.Null(healthySummary.Surface);
        Assert.Null(healthySummary.Notes);

        // Grouping per tooth: the history buckets by ToothNumber, and tooth 11
        // exists only in history (no condition row) — the marker case.
        Assert.Equal(3, result.Value.History.Count);
        Assert.DoesNotContain(result.Value.Teeth, t => t.ToothNumber == 11);

        var onTooth11 = result.Value.History.Where(h => h.ToothNumber == 11).ToList();
        Assert.Single(onTooth11);
        Assert.Equal(polishing.Id, onTooth11[0].TreatmentRecordId);

        var idsOnTooth26 = result.Value.History.Where(h => h.ToothNumber == 26).Select(h => h.TreatmentRecordId).ToList();
        Assert.Equal(2, idsOnTooth26.Count);
        Assert.Contains(rootCanal.Id, idsOnTooth26);
        Assert.Contains(filling.Id, idsOnTooth26);

        var mappedRootCanal = result.Value.History.Single(h => h.TreatmentRecordId == rootCanal.Id);
        Assert.Equal(doctor.Id, mappedRootCanal.DoctorId);
        Assert.Equal("Root Canal Treatment", mappedRootCanal.ProcedureDetails);
        Assert.Equal(260m, mappedRootCanal.Cost);
        Assert.NotEqual(default, mappedRootCanal.RecordedAtUtc);
    }

    [Fact]
    public async Task Handle_WithSoftDeletedToothRow_ShouldNotReturnItInTeeth()
    {
        // Arrange: a live row plus a soft-deleted row (the query filter on
        // PatientTeeth hides the deleted one; it must not block the list).
        var patient = await SeedPatientAsync();

        var liveRow = PatientTooth.Create(patient.Id, 26, ToothCondition.Filled).Value;
        await _context.PatientTeeth.AddAsync(liveRow);
        await _context.SaveChangesAsync(default);

        var deletedRow = PatientTooth.Create(patient.Id, 11, ToothCondition.Caries).Value;
        await _context.PatientTeeth.AddAsync(deletedRow);
        await _context.SaveChangesAsync(default);

        deletedRow.Delete();
        await _context.SaveChangesAsync(default);

        var query = new GetPatientOdontogramQuery(patient.Id, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var toothNumbers = result.Value.Teeth.Select(t => t.ToothNumber).ToList();
        Assert.Contains(26, toothNumbers);
        Assert.DoesNotContain(11, toothNumbers);
        Assert.Single(result.Value.Teeth);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorOutsidePatientScope_ShouldReturnPatientAccessForbidden()
    {
        // Arrange: a fresh patient with no appointment and a restricted scope
        // whose doctor has no link to the patient (fail-closed).
        var patient = await SeedPatientAsync();
        var query = new GetPatientOdontogramQuery(
            patient.Id,
            new PatientAccessScope(IsRestricted: true, DoctorId: Guid.NewGuid(), UserId: null));

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.PatientAccessForbidden.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorHavingAppointmentForPatient_ShouldReturnOdontogram()
    {
        // Arrange: the appointment link opens the patient for the restricted
        // doctor, who then sees the tooth rows written for that patient.
        var contactInfo = ContactInfo.Create("+963944444444", null, true, null).Value;
        var doctorContactInfo = ContactInfo.Create("+963966666666", null, true, null).Value;
        var patient = Patient.Create("Maha", "Idris", contactInfo, DateTime.UtcNow.AddYears(-28), Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", doctorContactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        var toothRow = PatientTooth.Create(patient.Id, 16, ToothCondition.Crown).Value;
        await _context.PatientTeeth.AddAsync(toothRow);
        await _context.SaveChangesAsync(default);

        var query = new GetPatientOdontogramQuery(
            patient.Id,
            new PatientAccessScope(IsRestricted: true, DoctorId: doctor.Id, UserId: null));

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(patient.Id, result.Value.PatientId);
        var summary = result.Value.Teeth.Single(t => t.ToothNumber == 16);
        Assert.Equal(ToothCondition.Crown, summary.Condition);
        Assert.Empty(result.Value.History);
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var contactInfo = ContactInfo.Create("+963933333333", null, true, null).Value;
        var patient = Patient.Create("Layla", "Haddad", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        return patient;
    }
}