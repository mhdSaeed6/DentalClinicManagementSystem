using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;
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

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.RescheduleAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RescheduleAppointmentCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRescheduleAppointmentAndSaveToDb()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var newDateTime = DateTime.UtcNow.AddDays(2);
        var newDuration = 45;
        var command = new RescheduleAppointmentCommand(appointment.Id, newDateTime, newDuration);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(newDuration, result.Value.DurationInMinutes);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        Assert.NotNull(dbAppointment);
        Assert.Equal(newDuration, dbAppointment.DurationInMinutes);
    }

    [Fact]
    public async Task Handle_WithNonExistingAppointmentId_ShouldReturnAppointmentNotFound()
    {
        // Arrange
        var command = new RescheduleAppointmentCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 30);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithOverlappingAppointmentForSameDoctor_ShouldReturnSlotConflict()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var blockerStart = DateTime.UtcNow.AddDays(3);

        // Another scheduled appointment for the same doctor from 10:00 to 10:30.
        var blocker = Appointment.Create(appointment.PatientId, appointment.DoctorId, appointment.ServiceId, blockerStart, 30, null).Value;
        await _context.Appointments.AddAsync(blocker);
        await _context.SaveChangesAsync(default);

        // Try to move the first appointment into the blocker's slot (overlaps).
        var command = new RescheduleAppointmentCommand(
            appointment.Id,
            blockerStart.AddMinutes(10),
            DurationInMinutes: 60);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(ApplicationErrors.AppointmentSlotConflict, result.Errors);

        // The appointment must be unchanged.
        var dbAppointment = await _context.Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.NotNull(dbAppointment);
        Assert.Equal(appointment.ScheduledDateTime, dbAppointment.ScheduledDateTime);
        Assert.Equal(appointment.DurationInMinutes, dbAppointment.DurationInMinutes);
    }

    [Fact]
    public async Task Handle_WhenReschedulingToItsOwnSlot_ShouldSucceed()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new RescheduleAppointmentCommand(
            appointment.Id,
            appointment.ScheduledDateTime,
            appointment.DurationInMinutes);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
    }

    private async Task<Appointment> SeedAppointmentAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "teeth", 30).Value;

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(2), 30, null).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return appointment;
    }
}