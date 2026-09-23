using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointments;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Queries.GetAppointments;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetAppointmentsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithPaginationAndFilters_ShouldReturnFilteredPaginatedList()
    {
        // Arrange
        var (patient, doctor, service) = await SeedEntitiesAsync();

        var appointment1 = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        var appointment2 = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(3), 30, null).Value;

        // موعد آخر بحالة Completed للتأكد من الفلترة بالحالة
        var appointment3 = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(5), 30, null).Value;
        appointment3.Complete();

        await _context.Appointments.AddRangeAsync(appointment1, appointment2, appointment3);
        await _context.SaveChangesAsync(default);

        var query = new GetAppointmentsQuery(
            PageNumber: 1,
            PageSize: 10,
            PatientId: patient.Id,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            Status: AppointmentStatus.Scheduled);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items!.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(AppointmentStatus.Scheduled, item.Status));
    }

    [Fact]
    public async Task Handle_WhenNoMatchingRecords_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new GetAppointmentsQuery(
            PageNumber: 1,
            PageSize: 10,
            PatientId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    private async Task<(Patient Patient, Doctor Doctor, DentalService Service)> SeedEntitiesAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return (patient, doctor, service);
    }
}