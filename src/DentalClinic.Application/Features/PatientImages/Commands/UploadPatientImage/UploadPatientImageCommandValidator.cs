using DentalClinic.Domain.Common.ValueObjects;

using FluentValidation;

namespace DentalClinic.Application.Features.PatientImages.Commands.UploadPatientImage;

public sealed class UploadPatientImageCommandValidator : AbstractValidator<UploadPatientImageCommand>
{
    public UploadPatientImageCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("Kind is not a valid image category.");

        RuleFor(x => x.ToothNumber)
            .Must(tooth => !tooth.HasValue || FdiTooth.IsValidToothNumber(tooth.Value))
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("The image file name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("The image content type is required.");
    }
}