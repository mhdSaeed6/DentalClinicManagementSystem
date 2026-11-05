using FluentValidation;

namespace DentalClinic.Application.Features.Clinics.Commands.AddExchangeRate;

/// <summary>
/// Shape validation only. Semantic rules (three-letter codes, distinct pair,
/// positive rate) live in the domain aggregate so they hold for every caller.
/// </summary>
public sealed class AddExchangeRateCommandValidator : AbstractValidator<AddExchangeRateCommand>
{
    public AddExchangeRateCommandValidator()
    {
        RuleFor(x => x.FromCurrency)
            .NotEmpty().WithMessage("The source currency is required.")
            .Length(3).WithMessage("The source currency must be a three-letter code.");

        RuleFor(x => x.ToCurrency)
            .NotEmpty().WithMessage("The target currency is required.")
            .Length(3).WithMessage("The target currency must be a three-letter code.");

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("The rate must be greater than zero.");
    }
}