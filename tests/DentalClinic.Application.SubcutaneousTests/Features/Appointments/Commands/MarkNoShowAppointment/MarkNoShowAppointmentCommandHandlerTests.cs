using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.MarkNoShowAppointment;
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

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.MarkNoShowAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class MarkNoShowAppointmentCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingScheduledAppointment_ShouldMarkAsNoShowAndSaveToDb()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var command = new MarkNoShowAppointmentCommand(appointment.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbAppointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        Assert.NotNull(dbAppointment);
        Assert.Equal(AppointmentStatus.NoShow, dbAppointment.Status);
    }

    [Fact]
    public async Task Handle_WithNonExistingAppointmentId_ShouldReturnAppointmentNotFound()
    {
        // Arrange
        var command = new MarkNoShowAppointmentCommand(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<Appointment> SeedAppointmentAsync()
    {
        var contactInfo = ContactInfo.Create("+963933333333", null, true, null).Value;
        var patient = Patient.Create("Nour", "Hassan", contactInfo, DateTime.UtcNow.AddYears(-28), Gender.Female).Value;
        var doctor = Doctor.Create("Layla", "Mahmoud", "Orthodontics", contactInfo, Gender.Female).Value;
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
