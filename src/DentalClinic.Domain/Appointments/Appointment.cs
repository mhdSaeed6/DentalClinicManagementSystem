using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;

namespace DentalClinic.Domain.Appointments;

public class Appointment : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public DentalService? Service { get; set; }
    public DateTime ScheduledDateTime { get; private set; }
    public int DurationInMinutes { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Appointment() { }

    private Appointment(
        Guid id,
        Guid patientId,
        Guid doctorId,
        Guid serviceId,
        DateTime scheduledDateTime,
        int durationInMinutes,
        string? notes)
        : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        ServiceId = serviceId;
        ScheduledDateTime = scheduledDateTime;
        DurationInMinutes = durationInMinutes;
        Notes = notes;
        Status = AppointmentStatus.Scheduled;
    }

    public static Result<Appointment> Create(
        Guid patientId,
        Guid doctorId,
        Guid serviceId,
        DateTime scheduledDateTime,
        int durationInMinutes,
        string? notes = null)
    {
        if (scheduledDateTime <= DateTime.UtcNow)
        {
            return AppointmentErrors.InvalidScheduledTime;
        }

        if (durationInMinutes <= 0)
        {
            return AppointmentErrors.InvalidDuration;
        }

        return new Appointment(
            Guid.NewGuid(),
            patientId,
            doctorId,
            serviceId,
            scheduledDateTime,
            durationInMinutes,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
    }

    public Result<Updated> Complete()
    {
        if (Status == AppointmentStatus.Completed)
        {
            return AppointmentErrors.AlreadyCompleted;
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            return AppointmentErrors.AlreadyCancelled;
        }

        Status = AppointmentStatus.Completed;
        return Result.Updated;
    }

    public Result<Updated> Cancel()
    {
        if (Status == AppointmentStatus.Completed)
        {
            return AppointmentErrors.AlreadyCompleted;
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            return AppointmentErrors.AlreadyCancelled;
        }

        Status = AppointmentStatus.Cancelled;
        return Result.Updated;
    }

    public Result<Updated> Reschedule(DateTime newScheduledDateTime, int newDurationInMinutes)
    {
        if (Status == AppointmentStatus.Completed)
        {
            return AppointmentErrors.AlreadyCompleted;
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            return AppointmentErrors.AlreadyCancelled;
        }

        if (newScheduledDateTime <= DateTime.UtcNow)
        {
            return AppointmentErrors.InvalidScheduledTime;
        }

        if (newDurationInMinutes <= 0)
        {
            return AppointmentErrors.InvalidDuration;
        }

        ScheduledDateTime = newScheduledDateTime;
        DurationInMinutes = newDurationInMinutes;

        return Result.Updated;
    }
}