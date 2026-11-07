using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Applies a <see cref="PatientAccessScope"/> to a patient query. Extracted so
/// the registry, the alert count and every aggregate answer the same
/// "which patients may this caller see?" question identically.
/// </summary>
public static class PatientScopeExtensions
{
    /// <summary>
    /// Restricts the query to patients created by the scoped account or booked
    /// with the scoped doctor. An unrestricted scope passes through untouched;
    /// a restricted one with no doctor id matches only the creator branch,
    /// which fails closed for accounts with no linked profile.
    /// </summary>
    public static IQueryable<Patient> ApplyScope(
        this IQueryable<Patient> query,
        IAppDbContext context,
        PatientAccessScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (!scope.IsRestricted)
        {
            return query;
        }

        var scopeDoctorId = scope.DoctorId ?? Guid.Empty;
        var scopeUserId = scope.UserId;

        if (!string.IsNullOrEmpty(scopeUserId))
        {
            var userId = scopeUserId;

            return query.Where(patient =>
                patient.CreatedBy == userId ||
                context.Appointments.Any(a => a.PatientId == patient.Id && a.DoctorId == scopeDoctorId));
        }

        return query.Where(patient =>
            context.Appointments.Any(a => a.PatientId == patient.Id && a.DoctorId == scopeDoctorId));
    }
}