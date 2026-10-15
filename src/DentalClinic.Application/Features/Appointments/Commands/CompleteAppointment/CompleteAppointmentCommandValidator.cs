using DentalClinic.Domain.TreatmentRecords;

using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.");

        // Service replacement is optional. When a value is supplied it must be a
        // valid, non-empty identifier; existence/active-state is checked in the
        // handler. Omitting it leaves the appointment's service unchanged.
        When(x => x.ServiceId.HasValue, () =>
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty().WithMessage("ServiceId must be a valid identifier when provided.");
        });

        // Treatments are optional: an appointment may be completed as a
        // consultation-only visit with no treatment records.
        RuleForEach(x => x.Treatments).ChildRules(treatment =>
        {
            treatment.RuleFor(x => x.ToothNumber)
                .Must(TreatmentRecord.IsValidToothNumber)
                .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

            treatment.RuleFor(x => x.ProcedureDetails)
                .NotEmpty().WithMessage("Procedure details are required.")
                .MaximumLength(1000).WithMessage("Procedure details must not exceed 1000 characters.");

            treatment.RuleFor(x => x.Cost)
                .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
        });
    }
}
