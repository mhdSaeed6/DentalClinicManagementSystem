using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Commands.MarkOverdueAppointmentsAsNoShow;
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

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.MarkOverdueAppointmentsAsNoShow;

[Collection(WebAppFactoryCollection.CollectionName)]
public class MarkOverdueAppointmentsAsNoShowCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_ShouldMarkEndedAppointmentsAsNoShow_AndLeaveFutureOnesUntouched()
    {
        // Arrange
        var (ended, future) = await SeedAppointmentsAsync();

        // The ended appointment runs now+5m .. now+35m, the future one starts in five days.
        // A cutoff of now+45m (as-of now+1h with a 15m grace) catches only the ended one.
        var command = new MarkOverdueAppointmentsAsNoShowCommand(
            AsOfUtc: DateTime.UtcNow.AddHours(1),
            GracePeriod: TimeSpan.FromMinutes(15));

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value >= 1);

        var dbEnded = await _context.Appointments.AsNoTracking().FirstAsync(a => a.Id == ended.Id);
        var dbFuture = await _context.Appointments.AsNoTracking().FirstAsync(a => a.Id == future.Id);

        Assert.Equal(AppointmentStatus.NoShow, dbEnded.Status);
        Assert.Equal(AppointmentStatus.Scheduled, dbFuture.Status);
    }

    [Fact]
    public async Task Handle_ShouldMarkNothing_WhenNoAppointmentHasEnded()
    {
        // Arrange
        await SeedAppointmentsAsync();

        // A cutoff far in the past cannot match any (future) scheduled appointment.
        var command = new MarkOverdueAppointmentsAsNoShowCommand(
            AsOfUtc: DateTime.UtcNow,
            GracePeriod: TimeSpan.FromDays(365));

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    private async Task<(Appointment Ended, Appointment Future)> SeedAppointmentsAsync()
    {
        var contactInfo = ContactInfo.Create("+963944444444", null, true, null).Value;
        var patient = Patient.Create("Rami", "Khaled", contactInfo, DateTime.UtcNow.AddYears(-35), Gender.Male).Value;
        var doctor = Doctor.Create("Huda", "Nasser", "Endodontics", contactInfo, Gender.Female).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        var ended = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddMinutes(5), 30, null).Value;
        var future = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddDays(5), 30, null).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.Appointments.AddAsync(ended);
        await _context.Appointments.AddAsync(future);
        await _context.SaveChangesAsync(default);

        return (ended, future);
    }
}
