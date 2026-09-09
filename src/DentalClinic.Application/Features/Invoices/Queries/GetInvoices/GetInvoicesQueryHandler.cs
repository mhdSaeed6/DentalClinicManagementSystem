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
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<InvoiceDto>
        {
            Items = invoices.ToDtos(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
