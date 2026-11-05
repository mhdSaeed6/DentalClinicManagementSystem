using FluentValidation;

namespace DentalClinic.Application.Features.Clinics.Commands.UpdateClinicSettings;

/// <summary>
/// Shape validation only. Semantic rules (is the timezone real, is the currency
/// a valid ISO code, does the secondary differ from the base) live in the
/// aggregate so they hold no matter who calls <c>Update</c>.
/// </summary>
public sealed class UpdateClinicSettingsCommandValidator : AbstractValidator<UpdateClinicSettingsCommand>
{
    public UpdateClinicSettingsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(120).WithMessage("Clinic name must not exceed 120 characters.");

        RuleFor(x => x.NameArabic)
            .NotEmpty().WithMessage("Arabic clinic name is required.")
            .MaximumLength(120).WithMessage("Arabic clinic name must not exceed 120 characters.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(400).WithMessage("Logo URL must not exceed 400 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(40).WithMessage("Phone must not exceed 40 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid address.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Timezone is required.")
            .MaximumLength(100).WithMessage("Timezone id must not exceed 100 characters.");

        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a three-letter code.");

        RuleFor(x => x.SecondaryCurrency)
            .Length(3).WithMessage("Secondary currency must be a three-letter code.")
            .When(x => !string.IsNullOrWhiteSpace(x.SecondaryCurrency));

        RuleFor(x => x.DefaultCountryCode)
            .MaximumLength(6).WithMessage("Country code must not exceed 6 characters.");
    }
}