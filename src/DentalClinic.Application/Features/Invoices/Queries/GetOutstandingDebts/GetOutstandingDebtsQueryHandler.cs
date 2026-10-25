using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Invoices.Queries.GetOutstandingDebts;

public class GetOutstandingDebtsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetOutstandingDebtsQuery, Result<PaginatedList<OutstandingDebtDto>>>
{
    public async Task<Result<PaginatedList<OutstandingDebtDto>>> Handle(GetOutstandingDebtsQuery request, CancellationToken cancellationToken)
    {
        // One row per patient with money owed on issued invoices. Drafts are
        // estimates rather than real debt, so they never count as debt here.
        var query = context.Invoices
            .AsNoTracking()
            .Where(i => i.LifecycleStatus == InvoiceLifecycleStatus.Issued)
            .Where(i => i.TotalAmount - i.PaidAmount > 0);

        if (request.MinAmount.HasValue)
        {
            query = query.Where(i => i.TotalAmount - i.PaidAmount >= request.MinAmount.Value);
        }

        // Same trusted, server-derived scope as the invoice list, applied
        // before any client-supplied filter.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            query = query.Where(i =>
                i.AppointmentId != null &&
                context.Appointments.Any(a => a.Id == i.AppointmentId && a.DoctorId == scopeDoctorId));
        }

        // The grouped set is at most one row per indebted patient — small for
        // a clinic — so the aggregation is materialised in full and ordered /
        // paged in memory. Paging directly over a GROUP BY is not translatable.
        var debts = await query
            .GroupBy(i => i.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                OpenInvoiceCount = g.Count(),
                TotalOutstanding = g.Sum(i => i.TotalAmount - i.PaidAmount),
                OldestOpenInvoiceDateUtc = (DateTimeOffset?)g.Min(i => i.CreatedAtUtc),
            })
            .ToListAsync(cancellationToken);

        var totalCount = debts.Count;

        var page = debts
            .OrderByDescending(d => d.TotalOutstanding)
            .ThenBy(d => d.OldestOpenInvoiceDateUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Resolve patient names in a single round-trip so the UI never renders
        // "Unknown patient".
        var pagePatientIds = page.Select(d => d.PatientId).ToList();
        var patientNames = pagePatientIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Patients
                .AsNoTracking()
                .Where(p => pagePatientIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return new PaginatedList<OutstandingDebtDto>
        {
            Items = page
                .Select(d => new OutstandingDebtDto(
                    d.PatientId,
                    patientNames.GetValueOrDefault(d.PatientId, string.Empty),
                    d.OpenInvoiceCount,
                    d.TotalOutstanding,
                    d.OldestOpenInvoiceDateUtc))
                .ToList(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
