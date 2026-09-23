using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Enums;

namespace DentalClinic.Tests.Common.Builders;

public class AppointmentTestDataBuilder : ITestDataBuilder<Appointment>
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly int _durationInMinutes = 30;
    private readonly string? _notes = "Standard checkup";
    private Guid _patientId = Guid.NewGuid();
    private Guid _doctorId = Guid.NewGuid();
    private Guid _serviceId = Guid.NewGuid();
    private DateTime _appointmentDate = DateTime.UtcNow.AddDays(1);
    private AppointmentStatus _status = AppointmentStatus.Scheduled;

    public static AppointmentTestDataBuilder Create() => new();

    public AppointmentTestDataBuilder WithPatient(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public AppointmentTestDataBuilder WithDoctor(Guid doctorId)
    {
        _doctorId = doctorId;
        return this;
    }

    public AppointmentTestDataBuilder WithService(Guid serviceId)
    {
        _serviceId = serviceId;
        return this;
    }

    public AppointmentTestDataBuilder ScheduledAt(DateTime date)
    {
        _appointmentDate = date;
        return this;
    }

    public AppointmentTestDataBuilder Completed()
    {
        _status = AppointmentStatus.Completed;
        return this;
    }

    public AppointmentTestDataBuilder Cancelled()
    {
        _status = AppointmentStatus.Cancelled;
        return this;
    }

    public Appointment Build()
    {
        var appointment = Appointment.Create(
            _patientId,
            _doctorId,
            _serviceId,
            _appointmentDate,
            _durationInMinutes,
            _notes).Value;

        if (_status == AppointmentStatus.Completed)
        {
            appointment.Complete();
        }
        else if (_status == AppointmentStatus.Cancelled)
        {
            appointment.Cancel();
        }

        return appointment;
    }
}