namespace DentalClinic.Application.Features.TreatmentPlans.Mappers;

/// <summary>Aggregate money totals for a plan, computed over its items.</summary>
public readonly record struct TreatmentPlanTotals(int ItemCount, decimal EstimatedTotal, decimal PerformedTotal);