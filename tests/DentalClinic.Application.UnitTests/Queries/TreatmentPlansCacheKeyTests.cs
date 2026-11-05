using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class GetTreatmentPlansQueryTests
{
    private static readonly DoctorAccessScope Unrestricted = new(IsRestricted: false, Guid.Empty);

    private static readonly DoctorAccessScope DoctorA = new(IsRestricted: true, DoctorId: Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly DoctorAccessScope DoctorB = new(IsRestricted: true, DoctorId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void CacheKey_ShouldDifferPerScope()
    {
        // P1.2: the doctor scope must be part of the cache key, or one doctor's
        // plans could be served from another doctor's cached entry.
        var unrestricted = new GetTreatmentPlansQuery(Unrestricted, PatientId: Guid.NewGuid());
        var doctorA = new GetTreatmentPlansQuery(DoctorA, PatientId: unrestricted.PatientId);

        Assert.NotEqual(unrestricted.CacheKey, doctorA.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerDoctor()
    {
        var doctorA = new GetTreatmentPlansQuery(DoctorA);
        var doctorB = new GetTreatmentPlansQuery(DoctorB);

        Assert.NotEqual(doctorA.CacheKey, doctorB.CacheKey);
        Assert.Contains(DoctorA.DoctorId.ToString(), doctorA.CacheKey);
        Assert.Contains(DoctorB.DoctorId.ToString(), doctorB.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldBeStable_ForIdenticalRequests()
    {
        var first = new GetTreatmentPlansQuery(DoctorA, PageNumber: 2, PageSize: 25);
        var second = new GetTreatmentPlansQuery(DoctorA, PageNumber: 2, PageSize: 25);

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerFilters()
    {
        var unfiltered = new GetTreatmentPlansQuery(DoctorA);
        var byPatient = new GetTreatmentPlansQuery(DoctorA, PatientId: Guid.NewGuid());
        var byDoctor = new GetTreatmentPlansQuery(DoctorA, DoctorId: Guid.NewGuid());
        var byStatus = new GetTreatmentPlansQuery(DoctorA, Status: Domain.TreatmentPlans.TreatmentPlanStatus.Active);

        Assert.NotEqual(unfiltered.CacheKey, byPatient.CacheKey);
        Assert.NotEqual(unfiltered.CacheKey, byDoctor.CacheKey);
        Assert.NotEqual(unfiltered.CacheKey, byStatus.CacheKey);
    }

    [Fact]
    public void Tags_ShouldCarryPlanAndRecordTags()
    {
        // Items are treatment records — mutations on either side must clear
        // plan query caches.
        var query = new GetTreatmentPlansQuery(DoctorA);

        Assert.Contains("treatment-plan", query.Tags);
        Assert.Contains("treatment-record", query.Tags);
    }
}