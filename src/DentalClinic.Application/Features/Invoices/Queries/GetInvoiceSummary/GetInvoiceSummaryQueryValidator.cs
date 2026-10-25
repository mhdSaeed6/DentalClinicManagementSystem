using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;

public sealed class GetInvoiceSummaryQueryValidator : AbstractValidator<GetInvoiceSummaryQuery>
{
    public GetInvoiceSummaryQueryValidator()
    {
        // Nothing to bound today; kept alongside the other query validators so
        // the pattern holds if filters are added later.
    }
}
