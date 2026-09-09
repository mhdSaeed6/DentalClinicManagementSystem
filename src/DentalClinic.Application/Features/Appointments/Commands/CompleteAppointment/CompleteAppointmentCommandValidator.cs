using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.");
    }
}
