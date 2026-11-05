using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

public static class TreatmentPlanFormatting
{
    /// <summary>
    /// Groups plan items by session for the drawer and the print sheet.
    /// Sessions and items are ordered deterministically; cancelled items stay
    /// visible as history but contribute nothing to the subtotals.
    /// </summary>
    public static IReadOnlyList<TreatmentPlanSessionGroup> GroupBySession(IEnumerable<TreatmentPlanItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return [.. items
            .GroupBy(i => i.SessionNumber)
            .OrderBy(g => g.Key)
            .Select(g => new TreatmentPlanSessionGroup(
                g.Key,
                [.. g
                    .OrderBy(i => i.ToothNumber ?? int.MaxValue)
                    .ThenBy(i => i.ProcedureDetails, StringComparer.OrdinalIgnoreCase)]))];
    }
}