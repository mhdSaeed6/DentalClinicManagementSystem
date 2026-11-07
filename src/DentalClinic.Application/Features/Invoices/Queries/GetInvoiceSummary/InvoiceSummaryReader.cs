using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;

/// <summary>
/// The single implementation of the invoice money maths: which invoices count,
/// how the caller's scope narrows them, and how the totals are summed.
/// </summary>
/// <remarks>
/// Shared by the invoice summary endpoint and the dashboard/report aggregates
/// so a report can never disagree with the billing screen, and so the sums stay
/// real SQL aggregates over the whole matching set — never a page.
/// </remarks>
internal static class InvoiceSummaryReader
{
    /// <summary>
    /// Invoices visible to the caller: every lifecycle state, narrowed by the
    /// trusted doctor scope and the optional patient filter.
    /// </summary>
    public static IQueryable<Invoice> Scoped(
        IAppDbContext context,
        DoctorAccessScope scope,
        Guid? patientId = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = context.Invoices.AsNoTracking();

        // Same trusted, server-derived scope as the invoice list, applied
        // before any client-supplied filter. A standalone invoice has no owning
        // doctor, so it stays out of a doctor's reach (fail-closed).
        if (scope.IsRestricted)
        {
            var scopeDoctorId = scope.DoctorId;
            query = query.Where(invoice =>
                invoice.AppointmentId != null &&
                context.Appointments.Any(a => a.Id == invoice.AppointmentId && a.DoctorId == scopeDoctorId));
        }

        if (patientId.HasValue)
        {
            query = query.Where(invoice => invoice.PatientId == patientId.Value);
        }

        return query;
    }

    /// <summary>
    /// The invoices that carry money. Drafts are estimates, not debt: only
    /// issued invoices take part in the totals.
    /// </summary>
    public static IQueryable<Invoice> Issued(
        IAppDbContext context,
        DoctorAccessScope scope,
        Guid? patientId = null) =>
        Scoped(context, scope, patientId)
            .Where(invoice => invoice.LifecycleStatus == InvoiceLifecycleStatus.Issued);

    /// <summary>Aggregates the supplied invoices into the shared summary shape.</summary>
    public static async Task<InvoiceSummaryDto> ReadAsync(
        IQueryable<Invoice> query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var totalInvoiceCount = await query.CountAsync(cancellationToken);
        var totalBilled = await query.SumAsync(invoice => invoice.TotalAmount, cancellationToken);
        var totalPaid = await query.SumAsync(invoice => invoice.PaidAmount, cancellationToken);

        var openQuery = query.Where(invoice => invoice.Status != PaymentStatus.Paid);
        var openInvoiceCount = await openQuery.CountAsync(cancellationToken);
        var oldestUnpaid = await openQuery
            .OrderBy(invoice => invoice.CreatedAtUtc)
            .Select(invoice => (DateTimeOffset?)invoice.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new InvoiceSummaryDto(
            TotalInvoiceCount: totalInvoiceCount,
            TotalBilled: totalBilled,
            TotalPaid: totalPaid,
            TotalOutstanding: totalBilled - totalPaid,
            OpenInvoiceCount: openInvoiceCount,
            OldestUnpaidInvoiceDateUtc: oldestUnpaid);
    }
}