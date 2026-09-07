using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments;

public static class AppointmentErrors
{
    public static readonly Error InvalidScheduledTime = Error.Validation(
        "Appointment.InvalidScheduledTime",
        "Scheduled time must be in the future.");

    public static readonly Error InvalidDuration = Error.Validation(
        "Appointment.InvalidDuration",
        "Duration must be greater than zero minutes.");

    public static readonly Error AlreadyCompleted = Error.Validation(
        "Appointment.AlreadyCompleted",
        "Cannot modify or cancel an appointment that is already completed.");

    public static readonly Error AlreadyCancelled = Error.Validation(
        "Appointment.AlreadyCancelled",
        "Cannot modify an appointment that is already cancelled.");
}