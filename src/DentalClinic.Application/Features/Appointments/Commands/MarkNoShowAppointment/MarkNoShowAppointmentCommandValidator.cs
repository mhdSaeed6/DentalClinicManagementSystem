using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.MarkNoShowAppointment;

public sealed class MarkNoShowAppointmentCommandValidator : AbstractValidator<MarkNoShowAppointmentCommand>
{
    public MarkNoShowAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.");
    }
}
