using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Queries.GetTreatmentRecords;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentRecordsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithFiltersAndPagination_ShouldReturnFilteredPaginatedList()
    {
        // Arrange
        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        var record1 = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Scaling & Polishing", 50, null).Value;
        var record2 = TreatmentRecord.Create(patient.Id, doctor.Id, 21, "Composite Filling", 100, null).Value;

        // سجل لمريض آخر للتأكد من عمل الفلترة
        var otherPatientContact = ContactInfo.Create("+963922222222", null, true, null).Value;
        var otherPatient = Patient.Create("Rami", "Nader", otherPatientContact, DateTime.UtcNow.AddYears(-20), Gender.Male).Value;
        await _context.Patients.AddAsync(otherPatient);
        var record3 = TreatmentRecord.Create(otherPatient.Id, doctor.Id, 11, "Extraction", 80, null).Value;

        await _context.TreatmentRecords.AddRangeAsync(record1, record2, record3);
        await _context.SaveChangesAsync(default);

        var query = new GetTreatmentRecordsQuery(
            Scope: DoctorAccessScope.None,
            PageNumber: 1,
            PageSize: 10,
            PatientId: patient.Id,
            DoctorId: doctor.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items!.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(patient.Id, item.PatientId));
    }

    [Fact]
    public async Task Handle_WhenNoMatchingRecords_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 10, PatientId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WithAppointmentFilter_ShouldReturnOnlyThatAppointmentsRecords()
    {
        // Arrange — two appointments for the same patient/doctor, with 2 and 1 records.
        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        var appointmentOne = Appointment.Create(patient.Id, doctor.Id, null, DateTime.UtcNow.AddHours(1), 30, null).Value;
        var appointmentTwo = Appointment.Create(patient.Id, doctor.Id, null, DateTime.UtcNow.AddHours(2), 30, null).Value;
        await _context.Appointments.AddRangeAsync(appointmentOne, appointmentTwo);

        var recordOneA = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Scaling & Polishing", 50, appointmentOne.Id).Value;
        var recordOneB = TreatmentRecord.Create(patient.Id, doctor.Id, 21, "Composite Filling", 100, appointmentOne.Id).Value;
        var recordTwo = TreatmentRecord.Create(patient.Id, doctor.Id, 11, "Extraction", 80, appointmentTwo.Id).Value;

        await _context.TreatmentRecords.AddRangeAsync(recordOneA, recordOneB, recordTwo);
        await _context.SaveChangesAsync(default);

        // Act — ask only for the first appointment's records.
        var result = await _mediator.Send(new GetTreatmentRecordsQuery(
            Scope: DoctorAccessScope.None,
            PageNumber: 1,
            PageSize: 10,
            AppointmentId: appointmentOne.Id));

        // Assert — the other appointment's record is never returned.
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items!, item => Assert.Equal(appointmentOne.Id, item.AppointmentId));
    }

    [Fact]
    public async Task Handle_WithAppointmentFilterWithoutRecords_ShouldReturnEmptyPaginatedList()
    {
        // Arrange — an appointment with no recorded treatments.
        var (patient, doctor) = await SeedPatientAndDoctorAsync();
        var appointment = Appointment.Create(patient.Id, doctor.Id, null, DateTime.UtcNow.AddHours(1), 30, null).Value;
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        // Act
        var result = await _mediator.Send(new GetTreatmentRecordsQuery(
            Scope: DoctorAccessScope.None,
            PageNumber: 1,
            PageSize: 10,
            AppointmentId: appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WithToothFilter_ShouldReturnOnlyThatToothsRecords()
    {
        // Arrange
        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        var molarOne = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Scaling & Polishing", 50, null).Value;
        var molarTwo = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Root canal", 200, null).Value;
        var incisor = TreatmentRecord.Create(patient.Id, doctor.Id, 21, "Composite Filling", 100, null).Value;

        await _context.TreatmentRecords.AddRangeAsync(molarOne, molarTwo, incisor);
        await _context.SaveChangesAsync(default);

        // Act — ask only for tooth 16's records.
        var result = await _mediator.Send(new GetTreatmentRecordsQuery(
            Scope: DoctorAccessScope.None,
            PageNumber: 1,
            PageSize: 10,
            ToothNumber: 16));

        // Assert — the incisor record is never returned.
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items!, item => Assert.Equal(16, item.ToothNumber));
    }

    [Fact]
    public async Task Handle_WithToothFilterCombinedWithPatientFilter_ShouldReturnOnlyThatPatientsToothRecords()
    {
        // Arrange — the same tooth treated for two different patients.
        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        var otherPatientContact = ContactInfo.Create("+963922222222", null, true, null).Value;
        var otherPatient = Patient.Create("Rami", "Nader", otherPatientContact, DateTime.UtcNow.AddYears(-20), Gender.Male).Value;
        await _context.Patients.AddAsync(otherPatient);

        var ownRecord = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Scaling & Polishing", 50, null).Value;
        var otherRecord = TreatmentRecord.Create(otherPatient.Id, doctor.Id, 16, "Composite Filling", 100, null).Value;

        await _context.TreatmentRecords.AddRangeAsync(ownRecord, otherRecord);
        await _context.SaveChangesAsync(default);

        // Act
        var result = await _mediator.Send(new GetTreatmentRecordsQuery(
            Scope: DoctorAccessScope.None,
            PageNumber: 1,
            PageSize: 10,
            PatientId: patient.Id,
            ToothNumber: 16));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.All(result.Value.Items!, item =>
        {
            Assert.Equal(patient.Id, item.PatientId);
            Assert.Equal(16, item.ToothNumber);
        });
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientAndDoctorAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Samer", "Ali", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;
        var doctor = Doctor.Create("Khaled", "Hassan", "Orthodontics", contactInfo, Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        return (patient, doctor);
    }
}