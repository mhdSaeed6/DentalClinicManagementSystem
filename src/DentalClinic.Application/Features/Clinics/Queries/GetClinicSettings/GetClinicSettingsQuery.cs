using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Clinics.Queries.GetClinicSettings;

/// <summary>
/// The clinic profile, cached for an hour. The cache tag is the singular entity
/// name so <c>UpdateClinicSettingsCommandHandler</c> can bust it in one call.
/// </summary>
public sealed record GetClinicSettingsQuery : ICachedQuery<Result<ClinicSettingsDto>>
{
    public string CacheKey => "clinic-settings";

    public string[] Tags => ["clinic-setting"];

    public TimeSpan Expiration => TimeSpan.FromHours(1);
}