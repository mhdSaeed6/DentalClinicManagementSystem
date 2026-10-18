namespace DentalClinic.Api.Infrastructure;

using DentalClinic.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Readiness probe that verifies the database is actually reachable, not merely
/// that the process started. Uses the application's own DbContext so it exercises
/// the same connection and interceptors as real traffic.
/// </summary>
public sealed class DatabaseHealthCheck(IAppDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reachable = await context.Patients.AnyAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                "Database reachable.",
                new Dictionary<string, object> { ["patientsProbe"] = reachable });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database is unreachable.",
                ex);
        }
    }
}