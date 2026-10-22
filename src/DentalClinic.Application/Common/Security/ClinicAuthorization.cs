using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Invoices;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Central place for the clinic's role and ownership rules. All entry points
/// (controllers, commands and queries) resolve authorization through these
/// helpers so that the same, fail-closed policy is applied everywhere and never
/// trusts client-supplied identifiers.
/// </summary>
public static class ClinicAuthorization
{
    /// <summary>
    /// Administrators and receptionists operate across every doctor.
    /// </summary>
    public static bool IsUnrestrictedStaff(this IUser user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Receptionist);

    /// <summary>
    /// Only administrators and doctors may create or modify clinical treatment
    /// records. Receptionists retain their invoicing permissions but must not
    /// author clinical data.
    /// </summary>
    public static bool CanWriteClinicalRecords(this IUser user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Doctor);

    /// <summary>
    /// Whether the caller may read or modify the appointment assigned to
    /// <paramref name="appointmentDoctorId"/>. Admins and receptionists may access
    /// any appointment; a doctor may only access their own.
    /// </summary>
    public static bool CanAccessAppointment(this IUser user, Guid appointmentDoctorId) =>
        user.IsUnrestrictedStaff() ||
        (user.IsInRole(Roles.Doctor) && user.DoctorId == appointmentDoctorId);

    /// <summary>
    /// Trusted doctor scope for appointment and treatment-record queries.
    /// </summary>
    public static DoctorAccessScope DoctorScope(this IUser user) =>
        user.IsUnrestrictedStaff()
            ? DoctorAccessScope.None
            : new DoctorAccessScope(IsRestricted: true, DoctorId: user.DoctorId ?? Guid.Empty);

    /// <summary>
    /// Trusted patient scope for patient queries. Every clinic staff member
    /// (administrators, doctors and receptionists) can see the full patient
    /// registry; only unrecognized identities fall back to the fail-closed
    /// ownership scope.
    /// </summary>
    public static PatientAccessScope PatientScope(this IUser user) =>
        user.IsUnrestrictedStaff() || user.IsInRole(Roles.Doctor)
            ? PatientAccessScope.None
            : new PatientAccessScope(IsRestricted: true, DoctorId: user.DoctorId, UserId: user.Id);

    /// <summary>
    /// Whether the caller may read or modify the supplied invoice. The rule
    /// mirrors the invoice read scope exactly, so a mutation can never reach
    /// further than a read: administrators and receptionists operate across
    /// every invoice, while a doctor may only touch an invoice that is linked
    /// to one of their own appointments. A standalone invoice has no owning
    /// doctor, so it stays out of a doctor's reach (fail-closed).
    /// </summary>
    public static async Task<bool> CanAccessInvoiceAsync(
        this IUser user,
        IAppDbContext context,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        if (user.IsUnrestrictedStaff())
        {
            return true;
        }

        if (!user.IsInRole(Roles.Doctor) || user.DoctorId is not { } doctorId)
        {
            return false;
        }

        if (invoice.AppointmentId is not { } appointmentId)
        {
            return false;
        }

        return await context.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.Id == appointmentId && a.DoctorId == doctorId, cancellationToken);
    }
}
