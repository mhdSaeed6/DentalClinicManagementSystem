using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;

public sealed record GetInvoiceSummaryQuery(
    DoctorAccessScope Scope,
    Guid? PatientId = null) : ICachedQuery<Result<InvoiceSummaryDto>>
{
    // The scope is part of the key: a doctor's summary must never serve
    // another doctor, and per-patient summaries must not leak between files.
    public string CacheKey => $"invoices-summary-patient-{PatientId?.ToString() ?? "all"}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["invoice"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
