namespace DentalClinic.Application.Features.TreatmentPlans.Dtos;

/// <summary>
/// Per-session aggregation for the print sheet: planned items contribute their
/// estimate, performed items their charged cost, cancelled items nothing.
/// </summary>
public sealed record TreatmentPlanSessionDto(
    int SessionNumber,
    int ItemCount,
    decimal EstimatedTotal,
    decimal PerformedTotal,
    decimal GrandTotal);