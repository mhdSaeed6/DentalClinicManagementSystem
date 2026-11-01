using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

/// <summary>
/// One printable session: its items plus money subtotals that mirror
/// GetTreatmentPlanSessionsQuery (planned items count their estimate,
/// performed items their charge, cancelled items nothing).
/// </summary>
public sealed record TreatmentPlanSessionGroup(int SessionNumber, IReadOnlyList<TreatmentPlanItemDto> Items)
{
    public decimal EstimatedTotal =>
        Items.Where(i => i.Status is TreatmentRecordStatus.Planned).Sum(i => i.EstimatedCost ?? 0m);

    public decimal PerformedTotal =>
        Items.Where(i => i.Status is TreatmentRecordStatus.Performed).Sum(i => i.Cost);

    public decimal GrandTotal => EstimatedTotal + PerformedTotal;
}