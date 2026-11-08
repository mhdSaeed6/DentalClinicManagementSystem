using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Queries.GetLabCases;
using DentalClinic.Domain.Labs;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class LabCasesCacheKeyTests
{
    private static readonly DoctorAccessScope Unrestricted = new(IsRestricted: false, Guid.Empty);

    private static readonly DoctorAccessScope DoctorA = new(IsRestricted: true, DoctorId: Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly DoctorAccessScope DoctorB = new(IsRestricted: true, DoctorId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void CacheKey_ShouldDifferPerScope()
    {
        // The doctor scope must be part of the cache key, or one doctor's lab
        // cases could be served from another doctor's cached entry.
        var unrestricted = new GetLabCasesQuery(Unrestricted, PatientId: Guid.NewGuid());
        var doctorA = new GetLabCasesQuery(DoctorA, PatientId: unrestricted.PatientId);

        Assert.NotEqual(unrestricted.CacheKey, doctorA.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerDoctor()
    {
        var doctorA = new GetLabCasesQuery(DoctorA);
        var doctorB = new GetLabCasesQuery(DoctorB);

        Assert.NotEqual(doctorA.CacheKey, doctorB.CacheKey);
        Assert.Contains(DoctorA.DoctorId.ToString(), doctorA.CacheKey);
        Assert.Contains(DoctorB.DoctorId.ToString(), doctorB.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldBeStable_ForIdenticalRequests()
    {
        var first = new GetLabCasesQuery(DoctorA, PageNumber: 2, PageSize: 25);
        var second = new GetLabCasesQuery(DoctorA, PageNumber: 2, PageSize: 25);

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerFilters()
    {
        var unfiltered = new GetLabCasesQuery(DoctorA);
        var byPatient = new GetLabCasesQuery(DoctorA, PatientId: Guid.NewGuid());
        var byDoctor = new GetLabCasesQuery(DoctorA, DoctorId: Guid.NewGuid());
        var byStatus = new GetLabCasesQuery(DoctorA, Status: LabCaseStatus.AtLab);
        var overdueOnly = new GetLabCasesQuery(DoctorA, Overdue: true);

        Assert.NotEqual(unfiltered.CacheKey, byPatient.CacheKey);
        Assert.NotEqual(unfiltered.CacheKey, byDoctor.CacheKey);
        Assert.NotEqual(unfiltered.CacheKey, byStatus.CacheKey);
        Assert.NotEqual(unfiltered.CacheKey, overdueOnly.CacheKey);
    }

    [Fact]
    public void Tags_ShouldCarryTheLabCaseTag()
    {
        var query = new GetLabCasesQuery(DoctorA);

        Assert.Contains("lab-case", query.Tags);
    }
}