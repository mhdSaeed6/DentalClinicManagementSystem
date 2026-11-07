using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Labs;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCases;

public sealed record GetLabCasesQuery(
    DoctorAccessScope Scope,
    int PageNumber = 1,
    int PageSize = 10,
    Guid? PatientId = null,
    Guid? DoctorId = null,
    LabCaseStatus? Status = null,
    bool? Overdue = null,
    DateTimeOffset? AsOfUtc = null) : ICachedQuery<Result<PaginatedList<LabCaseDto>>>
{
    public string CacheKey =>
        $"lab-cases-page-{PageNumber}-size-{PageSize}-patient-{PatientId?.ToString() ?? "all"}-doctor-{DoctorId?.ToString() ?? "all"}-status-{Status?.ToString() ?? "all"}-overdue-{Overdue?.ToString() ?? "all"}-asof-{AsOfUtc?.ToUnixTimeSeconds().ToString() ?? "now"}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["lab-case"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}