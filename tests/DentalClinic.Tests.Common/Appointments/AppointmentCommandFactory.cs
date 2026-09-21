using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;

namespace DentalClinic.Tests.Common.Appointments;

public static class AppointmentCommandFactory
{
    public static CreateAppointmentCommand CreateCreateAppointmentCommand()
    {
        return new CreateAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.Now.AddDays(1),
            15,
            "Test appointment");
    }
}