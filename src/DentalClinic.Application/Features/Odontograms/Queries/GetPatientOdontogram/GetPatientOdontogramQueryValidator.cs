using FluentValidation;

namespace DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;

public sealed class GetPatientOdontogramQueryValidator : AbstractValidator<GetPatientOdontogramQuery>
{
    public GetPatientOdontogramQueryValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithErrorCode("PatientId.Required")
            .WithMessage("Patient ID is required.");
    }
}