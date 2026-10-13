using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler(IAppDbContext context) : IRequestHandler<GetInvoicesQuery, Result<PaginatedList<InvoiceDto>>>
{
    public async Task<Result<PaginatedList<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Invoices.AsNoTracking();

        // 0. Trusted, server-derived scope. Applied before any client-supplied
        //    filter so a doctor only sees invoices linked to their own visits.
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

        if (request.AppointmentId.HasValue)
        {
            query = query.Where(i => i.AppointmentId == request.AppointmentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .Include(i => i.Payments)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Resolve patient names in a single round-trip so the UI never has to
        // render "Unknown patient".
        var patientIds = invoices.Select(i => i.PatientId).Distinct().ToList();
        var patientNames = patientIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Patients
                .Where(p => patientIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return new PaginatedList<InvoiceDto>
        {
            Items = invoices.ToDtos(patientNames),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
