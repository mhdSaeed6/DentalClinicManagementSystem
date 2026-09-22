using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Appointments;
using DentalClinic.Tests.Common.Appointments;

using Xunit;

namespace DentalClinic.Application.UnitTests.Mappers;

public class AppointmentMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;

        // Act
        var dto = appointment.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(appointment.Id, dto.Id);
        Assert.Equal(appointment.PatientId, dto.PatientId);
        Assert.Equal(appointment.DoctorId, dto.DoctorId);
        Assert.Equal(appointment.ServiceId, dto.ServiceId);
        Assert.Equal(appointment.ScheduledDateTime, dto.ScheduledDateTime);
        Assert.Equal(appointment.DurationInMinutes, dto.DurationInMinutes);
        Assert.Equal(appointment.Status, dto.Status);
        Assert.Equal(appointment.Notes, dto.Notes);
    }

    [Fact]
    public void ToDto_WhenEntityIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Appointment appointment = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => appointment.ToDto());
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var appointment1 = AppointmentFactory.CreateAppointment().Value;
        var appointment2 = AppointmentFactory.CreateAppointment().Value;
        var appointments = new List<Appointment> { appointment1, appointment2 };

        // Act
        var dtos = appointments.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);

        Assert.Equal(appointment1.Id, dtos[0].Id);
        Assert.Equal(appointment1.PatientId, dtos[0].PatientId);
        Assert.Equal(appointment1.DoctorId, dtos[0].DoctorId);

        Assert.Equal(appointment2.Id, dtos[1].Id);
        Assert.Equal(appointment2.PatientId, dtos[1].PatientId);
        Assert.Equal(appointment2.DoctorId, dtos[1].DoctorId);
    }

    [Fact]
    public void ToDtos_WhenListIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var appointments = new List<Appointment>();

        // Act
        var dtos = appointments.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
}