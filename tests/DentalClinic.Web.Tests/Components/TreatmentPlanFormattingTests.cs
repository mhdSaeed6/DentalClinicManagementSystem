using DentalClinic.Web.Api;
using DentalClinic.Web.Components;

namespace DentalClinic.Web.Tests.Components;

public sealed class TreatmentPlanFormattingTests
{
    [Fact]
    public void GroupBySession_OrdersSessionsAndItemsDeterministically()
    {
        var items = new[]
        {
            Item(session: 2, tooth: 36, status: TreatmentRecordStatus.Planned),
            Item(session: 1, tooth: 26, status: TreatmentRecordStatus.Planned),
            Item(session: 1, tooth: 16, status: TreatmentRecordStatus.Performed),
        };

        var groups = TreatmentPlanFormatting.GroupBySession(items);

        Assert.Equal([1, 2], groups.Select(g => g.SessionNumber));
        Assert.Equal([16, 26], groups[0].Items.Select(i => i.ToothNumber));
    }

    [Fact]
    public void GroupBySession_SubtotalsFollowPlanBillingSemantics()
    {
        var items = new[]
        {
            Item(session: 1, tooth: 16, status: TreatmentRecordStatus.Performed, cost: 150m, estimate: 200m),
            Item(session: 1, tooth: 17, status: TreatmentRecordStatus.Planned, cost: 0m, estimate: 80m),
            Item(session: 1, tooth: 18, status: TreatmentRecordStatus.Cancelled, cost: 0m, estimate: 999m),
        };

        var group = Assert.Single(TreatmentPlanFormatting.GroupBySession(items));

        Assert.Equal(80m, group.EstimatedTotal);
        Assert.Equal(150m, group.PerformedTotal);
        Assert.Equal(230m, group.GrandTotal);
        Assert.Equal(3, group.Items.Count);
    }

    [Fact]
    public void GroupBySession_NoItems_ReturnsEmptyList()
    {
        Assert.Empty(TreatmentPlanFormatting.GroupBySession([]));
    }

    [Fact]
    public void GroupBySession_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TreatmentPlanFormatting.GroupBySession(null!));
    }

    private static TreatmentPlanItemDto Item(
        int session,
        int? tooth,
        TreatmentRecordStatus status,
        decimal cost = 0m,
        decimal? estimate = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            ToothNumber = tooth,
            ProcedureDetails = $"Procedure {tooth}",
            Status = status,
            Cost = cost,
            EstimatedCost = estimate,
            SessionNumber = session,
        };
}