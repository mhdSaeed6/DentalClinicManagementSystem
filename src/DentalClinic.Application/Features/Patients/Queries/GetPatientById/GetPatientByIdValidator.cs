using FluentValidation;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatientById;

public sealed class GetPatientByIdValidator : AbstractValidator<GetPatientByIdQuery>
{
    public GetPatientByIdValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithErrorCode("PatientId.Required")
            .WithMessage("Patient ID is required.");
    }
}