using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;

public sealed class GenerateAppointmentInvoiceCommandValidator : AbstractValidator<GenerateAppointmentInvoiceCommand>
{
    public GenerateAppointmentInvoiceCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.");
    }
}
