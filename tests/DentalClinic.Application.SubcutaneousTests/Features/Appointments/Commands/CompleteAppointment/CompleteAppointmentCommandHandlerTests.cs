using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CompleteAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CompleteAppointmentCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_WithExistingAppointment_ShouldMarkAsCompletedAndSaveToDb()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(appointment.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        Assert.NotNull(dbAppointment);
        Assert.Equal(AppointmentStatus.Completed, dbAppointment.Status);
    }

    [Fact]
    public async Task Handle_WithNonExistingAppointmentId_ShouldReturnAppointmentNotFound()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithSingleTreatment_ShouldCompleteAndCreateLinkedRecord()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(11, "Composite filling", 50m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);

        var records = await _context.TreatmentRecords
            .AsNoTracking()
            .Where(t => t.AppointmentId == appointment.Id)
            .ToListAsync();

        var record = Assert.Single(records);
        Assert.Equal(appointment.PatientId, record.PatientId);
        Assert.Equal(appointment.DoctorId, record.DoctorId);
        Assert.Equal(11, record.ToothNumber);
        Assert.Equal("Composite filling", record.ProcedureDetails);
        Assert.Equal(50m, record.Cost);
    }

    [Fact]
    public async Task Handle_WithMultipleTreatments_ShouldCreateAllLinkedRecords()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(
            appointment.Id,
            [
                new CompleteAppointmentTreatment(11, "Composite filling", 50m),
                new CompleteAppointmentTreatment(36, "Extraction", 120m),
            ]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var records = await _context.TreatmentRecords
            .AsNoTracking()
            .Where(t => t.AppointmentId == appointment.Id)
            .ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.All(records, record =>
        {
            Assert.Equal(appointment.PatientId, record.PatientId);
            Assert.Equal(appointment.DoctorId, record.DoctorId);
        });
    }

    [Fact]
    public async Task Handle_WithNoTreatments_ShouldCompleteAsConsultationOnly()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(appointment.Id, []);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);

        var anyRecord = await _context.TreatmentRecords
            .AsNoTracking()
            .AnyAsync(t => t.AppointmentId == appointment.Id);
        Assert.False(anyRecord);
    }

    [Fact]
    public async Task Handle_WithInvalidToothNumber_ShouldFailAndPersistNothing()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(99, "Composite filling", 50m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Scheduled, dbAppointment!.Status);

        var anyRecord = await _context.TreatmentRecords
            .AsNoTracking()
            .AnyAsync(t => t.AppointmentId == appointment.Id);
        Assert.False(anyRecord);
    }

    [Fact]
    public async Task Handle_AfterFailedAttempt_RetryWithValidTreatments_ShouldCreateExactlyOnce()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();

        // Act – first attempt fails validation
        var failed = await _mediator.Send(new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(99, "Composite filling", 50m)]));
        Assert.True(failed.IsError);

        // Act – retry with a valid treatment
        var retried = await _mediator.Send(new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(11, "Composite filling", 50m)]));
        Assert.True(retried.IsSuccess);

        // Assert
        var count = await _context.TreatmentRecords
            .AsNoTracking()
            .CountAsync(t => t.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_RetryAfterSuccess_ShouldNotDuplicateTreatments()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(11, "Composite filling", 50m)]);

        // Act
        var first = await _mediator.Send(command);
        Assert.True(first.IsSuccess);

        var second = await _mediator.Send(command);

        // Assert
        Assert.True(second.IsError);
        Assert.Equal("Appointment.AlreadyCompleted", second.TopError.Code);

        var count = await _context.TreatmentRecords
            .AsNoTracking()
            .CountAsync(t => t.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_AsReceptionist_WithTreatments_ShouldCompleteAndCreateRecords()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        factory.CurrentUser.AsReceptionist();

        var command = new CompleteAppointmentCommand(
            appointment.Id,
            [new CompleteAppointmentTreatment(11, "Composite filling", 50m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var count = await _context.TreatmentRecords
            .AsNoTracking()
            .CountAsync(t => t.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_WithDifferentService_ShouldReplaceServiceAndLeaveDefinitionUnchanged()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var performedService = await SeedServiceAsync("Teeth Whitening", 75m);

        var command = new CompleteAppointmentCommand(appointment.Id, null, performedService.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);
        Assert.Equal(performedService.Id, dbAppointment.ServiceId);

        // Only the appointment's reference changes — the service definition is intact.
        var dbService = await _context.DentalServices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == performedService.Id);
        Assert.Equal("Teeth Whitening", dbService!.Name);
        Assert.Equal(75m, dbService.Price);
    }

    [Fact]
    public async Task Handle_WithoutService_ShouldKeepBookedService()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var bookedServiceId = appointment.ServiceId;

        // Act
        var result = await _mediator.Send(new CompleteAppointmentCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(bookedServiceId, dbAppointment!.ServiceId);
    }

    [Fact]
    public async Task Handle_WithUnknownService_ShouldFailAndNotComplete()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new CompleteAppointmentCommand(appointment.Id, null, Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Service.NotFound", result.TopError.Code);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Scheduled, dbAppointment!.Status);
    }

    [Fact]
    public async Task Handle_WithInactiveService_ShouldFailAndNotComplete()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var inactiveService = await SeedServiceAsync("Retired Service", 20m);
        inactiveService.Deactivate();
        await _context.SaveChangesAsync(default);

        var command = new CompleteAppointmentCommand(appointment.Id, null, inactiveService.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Service.ServiceInactive", result.TopError.Code);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Scheduled, dbAppointment!.Status);
    }

    private async Task<DentalService> SeedServiceAsync(string name, decimal price)
    {
        var service = DentalService.Create(name, "Test service", price).Value;

        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return service;
    }

    private async Task<Appointment> SeedAppointmentAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return appointment;
    }
}
