using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

public class GetTreatmentRecordByIdQueryHandler(
    ILogger<GetTreatmentRecordByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetTreatmentRecordByIdQuery, Result<TreatmentRecordDto>>
{
    public async Task<Result<TreatmentRecordDto>> Handle(GetTreatmentRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var treatmentRecord = await context.TreatmentRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.TreatmentRecordId, cancellationToken);

        if (treatmentRecord is null)
        {
            logger.LogWarning("Treatment record retrieval failed. Record {TreatmentRecordId} not found.", request.TreatmentRecordId);
            return ApplicationErrors.TreatmentRecordNotFound;
        }

        if (request.Scope.IsRestricted && treatmentRecord.DoctorId != request.Scope.DoctorId)
        {
            logger.LogWarning("Treatment record {TreatmentRecordId} is outside the caller's authorized scope.", request.TreatmentRecordId);
            return ApplicationErrors.TreatmentRecordAccessForbidden;
        }

        return treatmentRecord.ToDto();
    }
}
