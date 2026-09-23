using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
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