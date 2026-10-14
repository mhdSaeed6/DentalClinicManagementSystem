using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
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

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CreateAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateAppointmentCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAppointmentAndSaveToDb()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync();
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: "Regular Checkup");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == result.Value.Id);

        Assert.NotNull(dbAppointment);
        Assert.Equal(patient.Id, dbAppointment.PatientId);
        Assert.Equal(doctor.Id, dbAppointment.DoctorId);
        Assert.Equal(service.Id, dbAppointment.ServiceId);
    }

    [Fact]
    public async Task Handle_WithoutService_ShouldCreateAppointment()
    {
        // Arrange
        var (patient, doctor, _) = await SeedEntitiesAsync();
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: null,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: "No service selected");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.ServiceId);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == result.Value.Id);

        Assert.NotNull(dbAppointment);
        Assert.Equal(patient.Id, dbAppointment.PatientId);
        Assert.Equal(doctor.Id, dbAppointment.DoctorId);
        Assert.Null(dbAppointment.ServiceId);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatient_ShouldReturnPatientNotFound()
    {
        // Arrange
        var (_, doctor, service) = await SeedEntitiesAsync();
        var command = new CreateAppointmentCommand(
            PatientId: Guid.NewGuid(),
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctor_ShouldReturnDoctorNotFound()
    {
        // Arrange
        var (patient, _, service) = await SeedEntitiesAsync();
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: Guid.NewGuid(),
            ServiceId: service.Id,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithInactiveDoctor_ShouldReturnDoctorInactive()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync(isDoctorActive: false);
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistingService_ShouldReturnServiceNotFound()
    {
        // Arrange
        var (patient, doctor, _) = await SeedEntitiesAsync();
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: Guid.NewGuid(),
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithInactiveService_ShouldReturnServiceInactive()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync(isServiceActive: false);
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: DateTime.UtcNow.AddDays(1),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithOverlappingDoctorAppointment_ShouldReturnSlotConflict()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync();
        var slotStart = DateTime.UtcNow.AddDays(1);

        // Existing appointment for the same doctor from 10:00 to 10:30.
        var existing = Appointment.Create(patient.Id, doctor.Id, service.Id, slotStart, 30, null).Value;
        await _context.Appointments.AddAsync(existing);
        await _context.SaveChangesAsync(default);

        // New appointment requested 10:15 -> 11:15 (overlaps existing).
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: slotStart.AddMinutes(15),
            DurationInMinutes: 60,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(ApplicationErrors.AppointmentSlotConflict, result.Errors);

        // No extra appointment should have been persisted for this doctor.
        var appointmentCount = await _context.Appointments.AsNoTracking()
            .CountAsync(a => a.DoctorId == doctor.Id);
        Assert.Equal(1, appointmentCount);
    }

    [Fact]
    public async Task Handle_WithAdjacentAppointment_ShouldCreateAppointment()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync();
        var slotStart = DateTime.UtcNow.AddDays(1);

        // Existing appointment for the same doctor from 10:00 to 10:30.
        var existing = Appointment.Create(patient.Id, doctor.Id, service.Id, slotStart, 30, null).Value;
        await _context.Appointments.AddAsync(existing);
        await _context.SaveChangesAsync(default);

        // New appointment starts exactly when the existing one ends (no overlap).
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: slotStart.AddMinutes(30),
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Handle_WithSameTimeForDifferentDoctor_ShouldCreateAppointment()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync();
        var contactInfo = ContactInfo.Create("+963922222222", null, true, null).Value;
        var otherDoctor = Doctor.Create("Layla", "Hasan", "General", contactInfo, Gender.Female).Value;
        await _context.Doctors.AddAsync(otherDoctor);
        await _context.SaveChangesAsync(default);

        var slotStart = DateTime.UtcNow.AddDays(1);

        var existing = Appointment.Create(patient.Id, doctor.Id, service.Id, slotStart, 30, null).Value;
        await _context.Appointments.AddAsync(existing);
        await _context.SaveChangesAsync(default);

        // Same time slot, but for a different doctor.
        var command = new CreateAppointmentCommand(
            PatientId: patient.Id,
            DoctorId: otherDoctor.Id,
            ServiceId: service.Id,
            ScheduledDateTime: slotStart,
            DurationInMinutes: 30,
            Notes: null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    private async Task<(Patient Patient, Doctor Doctor, DentalService Service)> SeedEntitiesAsync(
        bool isDoctorActive = true,
        bool isServiceActive = true)
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;

        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;

        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        if (!isDoctorActive)
        {
            doctor.Deactivate();
        }

        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;
        if (!isServiceActive)
        {
            service.Deactivate();
        }

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return (patient, doctor, service);
    }
}