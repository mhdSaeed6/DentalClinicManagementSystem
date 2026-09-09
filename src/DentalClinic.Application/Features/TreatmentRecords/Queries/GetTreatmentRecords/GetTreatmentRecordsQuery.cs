using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;

public sealed record GetTreatmentRecordsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? PatientId = null,
    Guid? DoctorId = null,
    Guid? AppointmentId = null) : ICachedQuery<Result<PaginatedList<TreatmentRecordDto>>>
{
    public string CacheKey => $"treatment-records-page-{PageNumber}-size-{PageSize}-patient-{PatientId?.ToString() ?? "all"}-doctor-{DoctorId?.ToString() ?? "all"}-appointment-{AppointmentId?.ToString() ?? "all"}";

    public string[] Tags => ["treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
