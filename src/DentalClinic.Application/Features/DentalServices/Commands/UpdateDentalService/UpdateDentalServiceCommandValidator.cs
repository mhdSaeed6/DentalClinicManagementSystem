using FluentValidation;

namespace DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;

public sealed class UpdateDentalServiceCommandValidator : AbstractValidator<UpdateDentalServiceCommand>
{
    public UpdateDentalServiceCommandValidator()
    {
        RuleFor(x => x.DentalServiceId)
            .NotEmpty().WithMessage("DentalServiceId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(100).WithMessage("Service name must not exceed 100 characters.");

        When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        });

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to zero.");
    }
}
