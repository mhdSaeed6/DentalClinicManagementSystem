using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoices;

public sealed record GetInvoicesQuery(
    DoctorAccessScope Scope,
    int PageNumber = 1,
    int PageSize = 10,
    Guid? PatientId = null,
    Guid? AppointmentId = null,
    PaymentStatus? Status = null) : ICachedQuery<Result<PaginatedList<InvoiceDto>>>
{
    public string CacheKey => $"invoices-page-{PageNumber}-size-{PageSize}-patient-{PatientId?.ToString() ?? "all"}-appointment-{AppointmentId?.ToString() ?? "all"}-status-{Status?.ToString() ?? "all"}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["invoice"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
