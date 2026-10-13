using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointments;
using DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Authorization;

/// <summary>
/// Handler-level authorization checks. These bypass the HTTP layer, so they prove
/// the ownership rules live in the application layer too (defence in depth) and do
/// not depend solely on the controller policies. The shared <c>TestCurrentUser</c>
/// is mutated per test and reset to Admin on dispose.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class AppointmentAuthorizationHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task CompleteAppointment_AsDoctor_ForAnotherDoctorsAppointment_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, otherDoctorAppointment) = await SeedClinicDataAsync();
        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(new CompleteAppointmentCommand(otherDoctorAppointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.AccessForbidden", result.TopError.Code);
    }

    [Fact]
    public async Task CompleteAppointment_AsDoctor_ForOwnAppointment_ShouldSucceed()
    {
        // Arrange
        var (_, ownDoctor, appointment) = await SeedClinicDataAsync();
        factory.CurrentUser.AsDoctor(ownDoctor.Id);

        // Act
        var result = await _mediator.Send(new CompleteAppointmentCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CompleteAppointment_AsReceptionist_ForAnyAppointment_ShouldSucceed()
    {
        // Arrange
        var (_, _, appointment) = await SeedClinicDataAsync();
        factory.CurrentUser.AsReceptionist();

        // Act
        var result = await _mediator.Send(new CompleteAppointmentCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAppointment_AsDoctor_ForAnotherDoctor_ShouldReturnForbidden()
    {
        // Arrange
        var (patient, _, _) = await SeedClinicDataAsync();
        var otherDoctor = await SeedDoctorAsync();
        var service = await SeedServiceAsync();

        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        var command = new CreateAppointmentCommand(
            patient.Id, otherDoctor.Id, service.Id, DateTime.UtcNow.AddDays(1), 30, null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.AccessForbidden", result.TopError.Code);
    }

    [Fact]
    public async Task GetAppointmentById_AsDoctor_ForAnotherDoctorsAppointment_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, appointment) = await SeedClinicDataAsync();
        factory.CurrentUser.AsDoctor(Guid.NewGuid());
        var scope = factory.CurrentUser.DoctorScope();

        // Act
        var result = await _mediator.Send(new GetAppointmentByIdQuery(appointment.Id, scope));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.AccessForbidden", result.TopError.Code);
    }

    [Fact]
    public async Task GetAppointments_AsDoctor_ShouldOnlyReturnOwnAppointments()
    {
        // Arrange
        var (_, ownDoctor, ownAppointment) = await SeedClinicDataAsync();
        var otherDoctor = await SeedDoctorAsync();
        await SeedAppointmentAsync(otherDoctor.Id);

        factory.CurrentUser.AsDoctor(ownDoctor.Id);
        var scope = factory.CurrentUser.DoctorScope();

        // Act
        var result = await _mediator.Send(
            new GetAppointmentsQuery(scope, PageNumber: 1, PageSize: 100, DoctorId: null));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Items!, a => Assert.Equal(ownDoctor.Id, a.DoctorId));
        Assert.Contains(result.Value.Items!, a => a.Id == ownAppointment.Id);
    }

    [Fact]
    public async Task CreateTreatmentRecord_AsReceptionist_ShouldReturnClinicalWriteForbidden()
    {
        // Arrange
        var (patient, doctor, _) = await SeedClinicDataAsync();
        factory.CurrentUser.AsReceptionist();

        var command = new CreateTreatmentRecordCommand(
            patient.Id, doctor.Id, null, 11, "Receptionist must not author clinical records.", 50m);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("TreatmentRecord.ClinicalWriteForbidden", result.TopError.Code);
    }

    [Fact]
    public async Task CreateTreatmentRecord_AsDoctor_ForAnotherDoctor_ShouldReturnAccessForbidden()
    {
        // Arrange
        var (patient, _, _) = await SeedClinicDataAsync();
        var otherDoctor = await SeedDoctorAsync();
        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        var command = new CreateTreatmentRecordCommand(
            patient.Id, otherDoctor.Id, null, 11, "Attempting to attribute to another doctor.", 50m);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("TreatmentRecord.AccessForbidden", result.TopError.Code);
    }

    [Fact]
    public async Task CreateTreatmentRecord_AsDoctor_ForOwnProfile_ShouldSucceed()
    {
        // Arrange
        var (patient, ownDoctor, _) = await SeedClinicDataAsync();
        factory.CurrentUser.AsDoctor(ownDoctor.Id);

        var command = new CreateTreatmentRecordCommand(
            patient.Id, ownDoctor.Id, null, 12, "Composite filling.", 80m);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<(Patient Patient, Doctor OwnDoctor, Appointment Appointment)> SeedClinicDataAsync()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var service = await SeedServiceAsync();
        var appointment = await SeedAppointmentAsync(doctor.Id, patient.Id, service.Id);

        return (patient, doctor, appointment);
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        return patient;
    }

    private async Task<Doctor> SeedDoctorAsync()
    {
        var contactInfo = ContactInfo.Create("+963922222222", null, true, null).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        return doctor;
    }

    private async Task<DentalService> SeedServiceAsync()
    {
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return service;
    }

    private async Task<Appointment> SeedAppointmentAsync(Guid doctorId, Guid? patientId = null, Guid? serviceId = null)
    {
        var patient = patientId ?? (await SeedPatientAsync()).Id;
        var service = serviceId ?? (await SeedServiceAsync()).Id;

        var appointment = Appointment
            .Create(patient, doctorId, service, DateTime.UtcNow.AddHours(1), 30, null)
            .Value;

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return appointment;
    }
}
