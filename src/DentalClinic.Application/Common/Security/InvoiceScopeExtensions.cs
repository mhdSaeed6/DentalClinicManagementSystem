using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Invoices;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Common.Security;

/// <summary>
/// Applies a <see cref="DoctorAccessScope"/> to an invoice query. Extracted so
/// the invoice list, the money summary and the debt report all answer
/// "which invoices may this caller see?" with the same predicate instead of
/// three copies that can drift apart.
/// </summary>
/// <remarks>
/// <para>
/// A restricted doctor reaches only invoices linked to one of their own
/// appointments. An invoice with no appointment was created by hand at the
/// front desk and belongs to no doctor: the schema has no column that could
/// attribute it to one, so it stays out of a doctor's reach (fail-closed)
/// rather than being guessed from the patient or the creation date.
/// </para>
/// <para>
/// Keep this rule in step with <see cref="ClinicAuthorization.CanAccessInvoiceAsync"/>:
/// a read scope wider than the write check leaks rows the user may not touch,
/// and a narrower one hides invoices they can still pay.
/// </para>
/// </remarks>
public static class InvoiceScopeExtensions
{
    /// <summary>
    /// Narrows the query to the invoices the scope may read. An unrestricted
    /// scope (administrators and receptionists) passes through untouched; a
    /// restricted one with no linked doctor id matches nothing.
    /// </summary>
    public static IQueryable<Invoice> ApplyScope(
        this IQueryable<Invoice> query,
        IAppDbContext context,
        DoctorAccessScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (!scope.IsRestricted)
        {
            return query;
        }

        var scopeDoctorId = scope.DoctorId;

        return query.Where(invoice =>
            invoice.AppointmentId != null &&
            context.Appointments.Any(a => a.Id == invoice.AppointmentId && a.DoctorId == scopeDoctorId));
    }
}