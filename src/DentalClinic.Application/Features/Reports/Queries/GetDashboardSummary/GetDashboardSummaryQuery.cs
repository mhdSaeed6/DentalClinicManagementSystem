using System.Globalization;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Reports.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;

/// <summary>
/// The dashboard and reports aggregate. <paramref name="From"/> and
/// <paramref name="To"/> are clinic-local dates and inclusive; when both are
/// omitted the window is the clinic's rolling last 30 days.
/// </summary>
/// <remarks>
/// Both scopes are part of the cache key: a doctor must never be served the
/// clinic-wide figures, and a restricted identity must never be served an
/// unrestricted patient count.
/// </remarks>
public sealed record GetDashboardSummaryQuery(
    DoctorAccessScope Scope,
    PatientAccessScope PatientScope,
    DateOnly? From = null,
    DateOnly? To = null) : ICachedQuery<Result<DashboardSummaryDto>>
{
    public string CacheKey =>
        $"dashboard-summary-from-{From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "default"}" +
        $"-to-{To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "default"}" +
        $"-scope-{Scope.IsRestricted}-{Scope.DoctorId}" +
        $"-patient-scope-{PatientScope.IsRestricted}-{PatientScope.DoctorId}-{PatientScope.UserId}";

    /// <summary>
    /// Every mutation that changes a figure on this screen busts one of these
    /// entity tags, so the dashboard never shows yesterday's numbers.
    /// </summary>
    public string[] Tags =>
        ["dashboard-summary", "appointment", "invoice", "patient", "doctor", "service"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}