using FluentValidation;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogsForEntity;

public sealed class GetAuditLogsForEntityQueryValidator : AbstractValidator<GetAuditLogsForEntityQuery>
{
    public GetAuditLogsForEntityQueryValidator()
    {
        RuleFor(x => x.EntityName)
            .NotEmpty()
            .WithErrorCode("EntityName.Required")
            .WithMessage("Entity name is required.")
            .MaximumLength(128)
            .WithErrorCode("EntityName.TooLong")
            .WithMessage("Entity name must not exceed 128 characters.");

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithErrorCode("EntityId.Required")
            .WithMessage("Entity ID is required.");
    }
}
