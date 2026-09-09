using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : ICachedQuery<Result<InvoiceDto>>
{
    public string CacheKey => $"invoice-{InvoiceId}";

    public string[] Tags => ["invoice"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
