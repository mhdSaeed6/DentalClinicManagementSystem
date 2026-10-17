using FluentValidation;

using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointmentWithDraft;

public class CompleteAppointmentWithDraftCommandValidator : AbstractValidator<CompleteAppointmentWithDraftCommand>
{
    public CompleteAppointmentWithDraftCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty();

        RuleForEach(x => x.Treatments).ChildRules(treatment =>
        {
            treatment.RuleFor(t => t.ToothNumber)
                .InclusiveBetween(1, 32);

            treatment.RuleFor(t => t.ProcedureDetails)
                .NotEmpty()
                .MaximumLength(1000);

            treatment.RuleFor(t => t.Cost)
                .GreaterThanOrEqualTo(0);
        });
    }
}