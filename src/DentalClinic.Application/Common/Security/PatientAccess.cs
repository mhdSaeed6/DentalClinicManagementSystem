using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Patients;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Patient-specific ownership checks that require database access.
/// </summary>
public static class PatientAccess
{
    /// <summary>
    /// Determines whether the trusted scope permits access to the supplied patient.
    /// Unrestricted scopes (administrators and receptionists) may access every
    /// patient. A restricted scope may access patients created by the scope's user,
    /// or patients with at least one appointment assigned to the scope's doctor.
    /// </summary>
    public static async Task<bool> CanAccessAsync(
        IAppDbContext context,
        PatientAccessScope scope,
        Patient patient,
        CancellationToken cancellationToken)
    {
        if (!scope.IsRestricted)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(scope.UserId) && patient.CreatedBy == scope.UserId)
        {
            return true;
        }

        if (scope.DoctorId is not { } doctorId)
        {
            return false;
        }

        return await context.Appointments.AnyAsync(
            a => a.PatientId == patient.Id && a.DoctorId == doctorId,
            cancellationToken);
    }
}
