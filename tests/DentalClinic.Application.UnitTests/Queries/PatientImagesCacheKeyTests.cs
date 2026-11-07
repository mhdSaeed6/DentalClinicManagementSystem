using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Queries.GetPatientImages;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

public class PatientImagesCacheKeyTests
{
    private static readonly PatientAccessScope Unrestricted = PatientAccessScope.None;

    private static readonly PatientAccessScope ReceptionistFallback =
        new(IsRestricted: true, DoctorId: null, UserId: "user-a");

    [Fact]
    public void CacheKey_ShouldDifferPerScope()
    {
        // The caller scope must be part of the key, or a cached gallery could be
        // served to an account that is not allowed to see the patient.
        var patientId = Guid.NewGuid();

        var unrestricted = new GetPatientImagesQuery(patientId, Unrestricted);
        var restricted = new GetPatientImagesQuery(patientId, ReceptionistFallback);

        Assert.NotEqual(unrestricted.CacheKey, restricted.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldDifferPerPatientAndUser()
    {
        var first = new GetPatientImagesQuery(Guid.NewGuid(), ReceptionistFallback);
        var second = new GetPatientImagesQuery(Guid.NewGuid(), ReceptionistFallback);

        Assert.NotEqual(first.CacheKey, second.CacheKey);
        Assert.Contains(ReceptionistFallback.UserId!, first.CacheKey);
    }

    [Fact]
    public void CacheKey_ShouldBeStable_ForIdenticalRequests()
    {
        var patientId = Guid.NewGuid();

        var first = new GetPatientImagesQuery(patientId, ReceptionistFallback);
        var second = new GetPatientImagesQuery(patientId, ReceptionistFallback);

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void Tags_ShouldCarryThePatientImageTag()
    {
        var query = new GetPatientImagesQuery(Guid.NewGuid(), Unrestricted);

        Assert.Contains("patient-image", query.Tags);
    }
}