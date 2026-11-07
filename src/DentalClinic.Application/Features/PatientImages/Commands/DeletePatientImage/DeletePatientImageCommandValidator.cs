using FluentValidation;

namespace DentalClinic.Application.Features.PatientImages.Commands.DeletePatientImage;

public sealed class DeletePatientImageCommandValidator : AbstractValidator<DeletePatientImageCommand>
{
    public DeletePatientImageCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("ImageId is required.");
    }
}