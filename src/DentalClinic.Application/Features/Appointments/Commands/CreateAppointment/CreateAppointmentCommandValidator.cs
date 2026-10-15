using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");

        // Service selection is optional. When a value is supplied it must be a
        // valid, non-empty identifier; existence/active-state is checked in the handler.
        When(x => x.ServiceId.HasValue, () =>
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty().WithMessage("ServiceId must be a valid identifier when provided.");
        });

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date and time must be in the future.");

        RuleFor(x => x.DurationInMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");

        When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
        });
    }
}
