using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Queries.GetAppointmentById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetAppointmentByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingAppointment_ShouldReturnAppointmentDto()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync();
        var query = new GetAppointmentByIdQuery(appointment.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(appointment.Id, result.Value.Id);
        Assert.Equal(appointment.PatientId, result.Value.PatientId);
        Assert.Equal(appointment.DoctorId, result.Value.DoctorId);
    }

    [Fact]
    public async Task Handle_WithNonExistingAppointmentId_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetAppointmentByIdQuery(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<Appointment> SeedAppointmentAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "teeth", 30).Value;

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(2), 30, "Query Check").Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return appointment;
    }
}