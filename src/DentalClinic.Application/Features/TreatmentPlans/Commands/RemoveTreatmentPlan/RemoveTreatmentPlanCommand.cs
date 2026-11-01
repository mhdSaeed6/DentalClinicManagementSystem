using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;

// D-P5: soft delete only. When the plan already holds performed (potentially
// billed) items, the request must carry ConfirmPerformedItems — the alternative
// for removing an unwanted history is cancelling, which never deletes.
public sealed record RemoveTreatmentPlanCommand(
    Guid PlanId,
    bool ConfirmPerformedItems = false) : IRequest<Result<Deleted>>;