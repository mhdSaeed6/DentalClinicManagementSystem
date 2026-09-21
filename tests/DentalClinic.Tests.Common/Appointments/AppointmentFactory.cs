using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Tests.Common.Appointments;

public static class AppointmentFactory
{
    public static Result<Appointment> CreateAppointment(
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? serviceId = null,
        DateTime? scheduledDateTime = null,
        int durationInMinutes = 30,
        string? notes = null)
    {
        return Appointment.Create(
            patientId ?? Guid.NewGuid(),
            doctorId ?? Guid.NewGuid(),
            serviceId ?? Guid.NewGuid(),
            scheduledDateTime ?? DateTime.UtcNow.AddDays(1),
            durationInMinutes,
            notes);
    }
}