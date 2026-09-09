using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.");

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date and time must be in the future.");

        RuleFor(x => x.DurationInMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");
    }
}
