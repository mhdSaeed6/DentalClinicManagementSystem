using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlanItem;

// Soft removal: the record keeps its history position but stops appearing in
// plan totals and the print sheet (the global query filter drops it).
public sealed record RemoveTreatmentPlanItemCommand(
    Guid PlanId,
    Guid ItemId) : IRequest<Result<Deleted>>;