using FluentValidation;

namespace DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;

public sealed class RemoveDentalServiceCommandValidator : AbstractValidator<RemoveDentalServiceCommand>
{
    public RemoveDentalServiceCommandValidator()
    {
        RuleFor(x => x.DentalServiceId)
            .NotEmpty().WithMessage("DentalServiceId is required.");
    }
}
