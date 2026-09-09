using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

public sealed record GetTreatmentRecordByIdQuery(Guid TreatmentRecordId) : ICachedQuery<Result<TreatmentRecordDto>>
{
    public string CacheKey => $"treatment-record-{TreatmentRecordId}";

    public string[] Tags => ["treatment-record"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
