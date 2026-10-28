using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Domain.TreatmentRecords;

using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointmentWithDraft;

public class CompleteAppointmentWithDraftCommandValidator : AbstractValidator<CompleteAppointmentWithDraftCommand>
{
    public CompleteAppointmentWithDraftCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty();

        RuleForEach(x => x.Treatments).ChildRules(treatment =>
        {
            // S8: a treatment may have no tooth (null); a provided tooth must be
            // a valid FDI number. Aligned with the CompleteAppointment validator —
            // the old 1-32 range was neither FDI-correct nor null-aware.
            treatment.RuleFor(t => t.ToothNumber)
                .Must(tooth => !tooth.HasValue || TreatmentRecord.IsValidToothNumber(tooth.Value))
                .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

            treatment.RuleFor(t => t.ProcedureDetails)
                .NotEmpty()
                .MaximumLength(1000);

            treatment.RuleFor(t => t.Cost)
                .GreaterThanOrEqualTo(0);
        });
    }
}