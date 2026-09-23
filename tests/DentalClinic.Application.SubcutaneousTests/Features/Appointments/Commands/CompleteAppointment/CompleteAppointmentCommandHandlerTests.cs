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
public class CompleteAppointmentCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

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