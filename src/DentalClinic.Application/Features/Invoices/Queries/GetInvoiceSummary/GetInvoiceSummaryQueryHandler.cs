using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;

public class GetInvoiceSummaryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInvoiceSummaryQuery, Result<InvoiceSummaryDto>>
{
    public async Task<Result<InvoiceSummaryDto>> Handle(GetInvoiceSummaryQuery request, CancellationToken cancellationToken)
    {
        // Drafts are estimates, not debt: only issued invoices carry money.
        var query = context.Invoices
            .AsNoTracking()
            .Where(i => i.LifecycleStatus == InvoiceLifecycleStatus.Issued);

        // Same trusted, server-derived scope as the invoice list, applied
        // before any client-supplied filter.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            query = query.Where(i =>
                i.AppointmentId != null &&
                context.Appointments.Any(a => a.Id == i.AppointmentId && a.DoctorId == scopeDoctorId));
        }

        if (request.PatientId.HasValue)
        {
            query = query.Where(i => i.PatientId == request.PatientId.Value);
        }

        // Real aggregate sums over the whole matching set — never a page.
        var totalInvoiceCount = await query.CountAsync(cancellationToken);
        var totalBilled = await query.SumAsync(i => i.TotalAmount, cancellationToken);
        var totalPaid = await query.SumAsync(i => i.PaidAmount, cancellationToken);

        var openQuery = query.Where(i => i.Status != PaymentStatus.Paid);
        var openInvoiceCount = await openQuery.CountAsync(cancellationToken);
        var oldestUnpaid = await openQuery
            .OrderBy(i => i.CreatedAtUtc)
            .Select(i => (DateTimeOffset?)i.CreatedAtUtc)
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
