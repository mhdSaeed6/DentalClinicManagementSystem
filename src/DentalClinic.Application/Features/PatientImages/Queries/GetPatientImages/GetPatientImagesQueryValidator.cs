using FluentValidation;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImages;

public sealed class GetPatientImagesQueryValidator : AbstractValidator<GetPatientImagesQuery>
{
    public GetPatientImagesQueryValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");
    }
}