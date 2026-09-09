using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatients;

public sealed record GetPatientsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) : ICachedQuery<Result<PaginatedList<PatientDto>>>
{
    // تضمين متغيرات البحث والصفحة داخل الكاش لضمان عدم تداخل الصفحات
    public string CacheKey => $"patients-page-{PageNumber}-size-{PageSize}-search-{SearchTerm ?? "all"}";

    public string[] Tags => ["patients"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}