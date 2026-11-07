using FluentValidation;

namespace DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;

/// <summary>
/// A reversed window is the only way this query can be wrong: a single date
/// means "that day", both omitted means the default rolling window.
/// </summary>
public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(query => query.To)
            .Must((query, to) => !to.HasValue || !query.From.HasValue || to.Value >= query.From.Value)
            .WithMessage("The report end date cannot be before its start date.");
    }
}