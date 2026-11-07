using FluentValidation;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImageContent;

public sealed class GetPatientImageContentQueryValidator : AbstractValidator<GetPatientImageContentQuery>
{
    public GetPatientImageContentQueryValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("ImageId is required.");
    }
}