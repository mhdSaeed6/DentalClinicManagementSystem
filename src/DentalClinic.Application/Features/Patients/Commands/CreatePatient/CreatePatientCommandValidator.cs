using FluentValidation;

namespace DentalClinic.Application.Features.Patients.Commands.CreatePatient;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        // 1. Basic Patient Details
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.");

        // 2. ContactInfo Validation
        RuleFor(x => x.ContactInfo)
            .NotNull().WithMessage("Contact info is required.");

        RuleFor(x => x.ContactInfo.PrimaryPhone)
            .NotEmpty().WithMessage("Primary phone is required.")
            .Matches(@"^\+?\d{7,15}$").WithMessage("Primary phone must be a valid phone number.");

        When(x => !string.IsNullOrWhiteSpace(x.ContactInfo.SecondaryPhone), () =>
        {
            RuleFor(x => x.ContactInfo.SecondaryPhone)
                .Matches(@"^\+?\d{7,15}$").WithMessage("Secondary phone must be a valid phone number.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ContactInfo.SocialMediaLink), () =>
        {
            RuleFor(x => x.ContactInfo.SocialMediaLink)
                .MaximumLength(200).WithMessage("Social media link must not exceed 200 characters.");
        });

        // 3. MedicalHistory Validation
        RuleFor(x => x.MedicalHistory)
            .NotNull().WithMessage("Medical history is required.");

        When(x => !string.IsNullOrWhiteSpace(x.MedicalHistory.Allergies), () =>
        {
            RuleFor(x => x.MedicalHistory.Allergies)
                .MaximumLength(500).WithMessage("Allergies details must not exceed 500 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.MedicalHistory.Notes), () =>
        {
            RuleFor(x => x.MedicalHistory.Notes)
                .MaximumLength(1000).WithMessage("Medical notes must not exceed 1000 characters.");
        });
    }
}