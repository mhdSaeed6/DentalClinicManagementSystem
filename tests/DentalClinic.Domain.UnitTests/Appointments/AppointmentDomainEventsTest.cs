using DentalClinic.Domain.Appointments.Events;
using DentalClinic.Tests.Common.Appointments;

namespace DentalClinic.Domain.UnitTests.Appointments;

/// <summary>
/// Verifies that the <see cref="DentalClinic.Domain.Appointments.Appointment"/>
/// aggregate raises exactly one domain event per successful state change, and that
/// rejected transitions raise nothing. The events are the hook a future WhatsApp
/// notifier will subscribe to, so their contents and cardinality matter.
/// </summary>
public class AppointmentDomainEventsTest
{
    [Fact]
    public void Create_ShouldRaiseCreatedEvent_WithIdentifiersAndSchedule()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var appointment = AppointmentFactory
            .CreateAppointment(patientId, doctorId, serviceId, scheduledDateTime)
            .Value;

        // Assert
        var domainEvent = Assert.Single(appointment.DomainEvents);
        var created = Assert.IsType<AppointmentCreatedEvent>(domainEvent);

        Assert.Equal(appointment.Id, created.AppointmentId);
        Assert.Equal(patientId, created.PatientId);
        Assert.Equal(doctorId, created.DoctorId);
        Assert.Equal(serviceId, created.ServiceId);
        Assert.Equal(scheduledDateTime, created.ScheduledDateTime);
    }

    [Fact]
    public void Create_WithoutService_ShouldRaiseCreatedEvent_WithNullService()
    {
        // Act
        var appointment = AppointmentFactory.CreateAppointment(serviceId: null).Value;

        // Assert
        var created = Assert.IsType<AppointmentCreatedEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(appointment.Id, created.AppointmentId);
        Assert.Null(created.ServiceId);
    }

    [Fact]
    public void Complete_ShouldRaiseCompletedEvent()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.ClearDomainEvents();

        // Act
        var result = appointment.Complete();

        // Assert
        Assert.True(result.IsSuccess);

        var completed = Assert.IsType<AppointmentCompletedEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(appointment.Id, completed.AppointmentId);
        Assert.Equal(appointment.PatientId, completed.PatientId);
        Assert.Equal(appointment.DoctorId, completed.DoctorId);
    }

    [Fact]
    public void Cancel_ShouldRaiseCancelledEvent()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.ClearDomainEvents();

        // Act
        var result = appointment.Cancel();

        // Assert
        Assert.True(result.IsSuccess);

        var cancelled = Assert.IsType<AppointmentCancelledEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(appointment.Id, cancelled.AppointmentId);
    }

    [Fact]
    public void MarkAsNoShow_ShouldRaiseNoShowEvent()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.ClearDomainEvents();

        // Act
        var result = appointment.MarkAsNoShow();

        // Assert
        Assert.True(result.IsSuccess);

        var noShow = Assert.IsType<AppointmentMarkedAsNoShowEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(appointment.Id, noShow.AppointmentId);
    }

    [Fact]
    public void Reschedule_ShouldRaiseRescheduledEvent_WithPreviousAndNewTimes()
    {
        // Arrange
        var original = DateTime.UtcNow.AddDays(1);
        var appointment = AppointmentFactory.CreateAppointment(scheduledDateTime: original).Value;
        appointment.ClearDomainEvents();

        var newDateTime = DateTime.UtcNow.AddDays(4);

        // Act
        var result = appointment.Reschedule(newDateTime, 45);

        // Assert
        Assert.True(result.IsSuccess);

        var rescheduled = Assert.IsType<AppointmentRescheduledEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(original, rescheduled.PreviousScheduledDateTime);
        Assert.Equal(newDateTime, rescheduled.ScheduledDateTime);
    }

    [Fact]
    public void RejectedTransition_ShouldNotRaiseEvent()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.Complete();
        appointment.ClearDomainEvents();

        // Act: every one of these is rejected because the appointment is completed.
        var cancel = appointment.Cancel();
        var complete = appointment.Complete();
        var noShow = appointment.MarkAsNoShow();
        var reschedule = appointment.Reschedule(DateTime.UtcNow.AddDays(2), 30);

        // Assert
        Assert.True(cancel.IsError);
        Assert.True(complete.IsError);
        Assert.True(noShow.IsError);
        Assert.True(reschedule.IsError);
        Assert.Empty(appointment.DomainEvents);
    }

    [Fact]
    public void NoShow_ShouldBeTerminal_AndRaiseNoFurtherEvents()
    {
        // Arrange
        var appointment = AppointmentFactory.CreateAppointment().Value;
        appointment.MarkAsNoShow();
        appointment.ClearDomainEvents();

        // Act: NoShow must be a terminal state.
        var complete = appointment.Complete();
        var cancel = appointment.Cancel();
        var noShow = appointment.MarkAsNoShow();
        var reschedule = appointment.Reschedule(DateTime.UtcNow.AddDays(2), 30);

        // Assert
        Assert.True(complete.IsError);
        Assert.True(cancel.IsError);
        Assert.True(noShow.IsError);
        Assert.True(reschedule.IsError);
        Assert.Empty(appointment.DomainEvents);
    }
}
