using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Tests.Common.Appointments;

#pragma warning disable SA1124
namespace DentalClinic.Domain.UnitTests.Appointments;

public class AppointmentTest
{
    #region Create Tests

    [Fact]
    public void CreateAppointment_ShouldSucceed_WithValidParameters()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        var durationInMinutes = 30;
        var notes = "  Patient prefers morning session.  ";

        // Act
        var result = AppointmentFactory.CreateAppointment(
            patientId,
            doctorId,
            serviceId,
            scheduledDateTime,
            durationInMinutes,
            notes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var appointment = result.Value;
        Assert.Equal(patientId, appointment.PatientId);
        Assert.Equal(doctorId, appointment.DoctorId);
        Assert.Equal(serviceId, appointment.ServiceId);
        Assert.Equal(scheduledDateTime, appointment.ScheduledDateTime);
        Assert.Equal(durationInMinutes, appointment.DurationInMinutes);
        Assert.Equal("Patient prefers morning session.", appointment.Notes);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }

    [Fact]
    public void CreateAppointment_ShouldReturnError_WhenScheduledTimeIsInPast()
    {
        // Arrange
        var scheduledDateTime = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = AppointmentFactory.CreateAppointment(scheduledDateTime: scheduledDateTime);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.InvalidScheduledTime.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void CreateAppointment_ShouldReturnError_WhenDurationIsZeroOrNegative(int invalidDuration)
    {
        // Act
        var result = AppointmentFactory.CreateAppointment(durationInMinutes: invalidDuration);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.InvalidDuration.Code, result.TopError.Code);
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_ShouldSucceed_WhenAppointmentIsScheduled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;

        // Act
        var result = appointment.Complete();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void Complete_ShouldReturnError_WhenAppointmentIsAlreadyCompleted()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Complete();

        // Act
        var result = appointment.Complete();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCompleted.Code, result.TopError.Code);
    }

    [Fact]
    public void Complete_ShouldReturnError_WhenAppointmentIsCancelled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Cancel();

        // Act
        var result = appointment.Complete();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCancelled.Code, result.TopError.Code);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_ShouldSucceed_WhenAppointmentIsScheduled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;

        // Act
        var result = appointment.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public void Cancel_ShouldReturnError_WhenAppointmentIsAlreadyCancelled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Cancel();

        // Act
        var result = appointment.Cancel();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCancelled.Code, result.TopError.Code);
    }

    [Fact]
    public void Cancel_ShouldReturnError_WhenAppointmentIsCompleted()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Complete();

        // Act
        var result = appointment.Cancel();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCompleted.Code, result.TopError.Code);
    }

    #endregion

    #region Reschedule Tests

    [Fact]
    public void Reschedule_ShouldSucceed_WhenInputIsValidAndScheduled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        var newDateTime = DateTime.UtcNow.AddDays(3);
        var newDuration = 45;

        // Act
        var result = appointment.Reschedule(newDateTime, newDuration);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newDateTime, appointment.ScheduledDateTime);
        Assert.Equal(newDuration, appointment.DurationInMinutes);
    }

    [Fact]
    public void Reschedule_ShouldReturnError_WhenNewTimeIsInPast()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        var pastDateTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = appointment.Reschedule(pastDateTime, 30);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.InvalidScheduledTime.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Reschedule_ShouldReturnError_WhenNewDurationIsZeroOrNegative(int invalidDuration)
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        var futureDateTime = DateTime.UtcNow.AddDays(2);

        // Act
        var result = appointment.Reschedule(futureDateTime, invalidDuration);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.InvalidDuration.Code, result.TopError.Code);
    }

    [Fact]
    public void Reschedule_ShouldReturnError_WhenAppointmentIsCompleted()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Complete();

        // Act
        var result = appointment.Reschedule(DateTime.UtcNow.AddDays(2), 30);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCompleted.Code, result.TopError.Code);
    }

    [Fact]
    public void Reschedule_ShouldReturnError_WhenAppointmentIsCancelled()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Cancel();

        // Act
        var result = appointment.Reschedule(DateTime.UtcNow.AddDays(2), 30);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(AppointmentErrors.AlreadyCancelled.Code, result.TopError.Code);
    }

    #endregion
}