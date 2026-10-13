using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

public sealed record GetTreatmentRecordByIdQuery(Guid TreatmentRecordId, DoctorAccessScope Scope) : ICachedQuery<Result<TreatmentRecordDto>>
{
    public string CacheKey => $"treatment-record-{TreatmentRecordId}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
