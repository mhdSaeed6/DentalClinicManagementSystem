using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class GetDashboardSummaryQueryTests
{
    [Fact]
    public void CacheKey_ShouldDifferPerWindow()
    {
        // Two windows are two different reports; serving one for the other
        // would show the wrong month's money.
        var open = new GetDashboardSummaryQuery(DoctorAccessScope.None, PatientAccessScope.None);
        var november = new GetDashboardSummaryQuery(
            DoctorAccessScope.None,
            PatientAccessScope.None,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30));
        var october = new GetDashboardSummaryQuery(
            DoctorAccessScope.None,
            PatientAccessScope.None,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31));

        Assert.NotEqual(open.CacheKey, november.CacheKey);
        Assert.NotEqual(november.CacheKey, october.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerScope()
    {
        // A doctor must never be served the clinic-wide figures, and a
        // restricted patient scope must never be served an unrestricted count.
        var unrestricted = new GetDashboardSummaryQuery(DoctorAccessScope.None, PatientAccessScope.None);
        var doctorScoped = new GetDashboardSummaryQuery(
            new DoctorAccessScope(true, Guid.NewGuid()),
            PatientAccessScope.None);
        var patientScoped = new GetDashboardSummaryQuery(
            DoctorAccessScope.None,
            new PatientAccessScope(true, Guid.NewGuid(), "user-a"));

        Assert.NotEqual(unrestricted.CacheKey, doctorScoped.CacheKey);
        Assert.NotEqual(unrestricted.CacheKey, patientScoped.CacheKey);
        Assert.NotEqual(doctorScoped.CacheKey, patientScoped.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldBeStable_ForTheSameQuestion()
    {
        var scope = new DoctorAccessScope(true, Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"));
        var patientScope = new PatientAccessScope(true, scope.DoctorId, "user-a");

        var first = new GetDashboardSummaryQuery(scope, patientScope, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));
        var second = new GetDashboardSummaryQuery(scope, patientScope, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void Tags_ShouldCoverEveryEntityTheFiguresDependOn()
    {
        // Each mutating handler busts its own singular tag; missing one leaves
        // the dashboard showing yesterday's numbers.
        var query = new GetDashboardSummaryQuery(DoctorAccessScope.None, PatientAccessScope.None);

        Assert.Contains("dashboard-summary", query.Tags);
        Assert.Contains("appointment", query.Tags);
        Assert.Contains("invoice", query.Tags);
        Assert.Contains("patient", query.Tags);
        Assert.Contains("doctor", query.Tags);
        Assert.Contains("service", query.Tags);
    }
}