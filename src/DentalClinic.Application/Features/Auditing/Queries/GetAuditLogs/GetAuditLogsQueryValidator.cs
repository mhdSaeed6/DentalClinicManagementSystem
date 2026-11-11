using DentalClinic.Domain.Auditing;

using FluentValidation;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        // Same upper bound as every other list endpoint: an unbounded page size
        // lets a client pull the entire trail in one request.
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must not exceed 100.");

        // A typo in the entity name must not silently return an empty page:
        // the admin would read "no history" where the truth is "wrong filter".
        RuleFor(x => x.EntityName)
            .Must(name => string.IsNullOrWhiteSpace(name) ||
                          AuditEntityNames.TryNormalize(name, out _))
            .WithMessage("Entity name must be one of: Patient, Appointment, Invoice, TreatmentRecord.");

        RuleFor(x => x.Action)
            .Must(action => string.IsNullOrWhiteSpace(action) ||
                            Enum.TryParse<AuditAction>(action, ignoreCase: true, out _))
            .WithMessage("Action must be one of: Create, Update, Delete.");

        RuleFor(x => x.UserId)
            .MaximumLength(450)
            .WithMessage("User id must not exceed 450 characters.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("The start date must be on or before the end date.");
    }
}
