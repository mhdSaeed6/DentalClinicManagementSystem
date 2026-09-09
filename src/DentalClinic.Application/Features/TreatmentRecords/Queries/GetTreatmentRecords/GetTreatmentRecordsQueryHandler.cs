using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;

public class GetTreatmentRecordsQueryHandler(IAppDbContext context) : IRequestHandler<GetTreatmentRecordsQuery, Result<PaginatedList<TreatmentRecordDto>>>
{
    public async Task<Result<PaginatedList<TreatmentRecordDto>>> Handle(GetTreatmentRecordsQuery request, CancellationToken cancellationToken)
    {
        var query = context.TreatmentRecords.AsNoTracking();

        if (request.PatientId.HasValue)
        {
            query = query.Where(r => r.PatientId == request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);
        }

        if (request.AppointmentId.HasValue)
        {
            query = query.Where(r => r.AppointmentId == request.AppointmentId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<TreatmentRecordDto>
        {
            Items = records.ToDtos(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
