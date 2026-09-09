using FluentValidation;

namespace DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;

public sealed class UpdateDoctorCommandValidator : AbstractValidator<UpdateDoctorCommand>
{
    public UpdateDoctorCommandValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Specialization)
            .NotEmpty().WithMessage("Specialization is required.")
            .MaximumLength(100).WithMessage("Specialization must not exceed 100 characters.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.");

        RuleFor(x => x.ContactInfo)
            .NotNull().WithMessage("Contact info is required.");

        When(x => x.ContactInfo is not null, () =>
        {
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
        });
    }
}
