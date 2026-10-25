using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;

/// <summary>
/// One round trip giving the UI everything it renders — the current per-tooth
/// conditions plus every treatment record grouped by tooth, which also surfaces
/// the "history but no state yet" marker.
/// <para>
/// The scope components are part of the cache key even though the payload for a
/// given patient does not vary between permitted callers: a restricted doctor
/// must never be served another caller's cached entry, and a denied caller must
/// never be handed a successful one (fail-closed via distinct keys).
/// </para>
/// </summary>
public sealed record GetPatientOdontogramQuery(Guid PatientId, PatientAccessScope Scope) : ICachedQuery<Result<PatientOdontogramDto>>
{
    public string CacheKey => $"patient-odontogram-{PatientId}-scope-{Scope.IsRestricted}-{Scope.DoctorId?.ToString() ?? "all"}-{Scope.UserId ?? "all"}";

    public string[] Tags => ["patient-tooth", "treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}