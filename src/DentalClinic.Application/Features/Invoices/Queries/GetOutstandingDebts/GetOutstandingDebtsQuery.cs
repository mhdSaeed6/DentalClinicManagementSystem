using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Invoices.Queries.GetOutstandingDebts;

public sealed record GetOutstandingDebtsQuery(
    DoctorAccessScope Scope,
    decimal? MinAmount = null,
    int PageNumber = 1,
    int PageSize = 10) : ICachedQuery<Result<PaginatedList<OutstandingDebtDto>>>
{
    public string CacheKey => $"outstanding-debts-page-{PageNumber}-size-{PageSize}-min-{MinAmount?.ToString() ?? "any"}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["invoice"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
