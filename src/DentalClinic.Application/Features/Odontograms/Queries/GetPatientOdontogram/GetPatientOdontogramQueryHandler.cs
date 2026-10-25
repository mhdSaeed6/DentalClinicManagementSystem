using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Application.Features.Odontograms.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;

public class GetPatientOdontogramQueryHandler(
    ILogger<GetPatientOdontogramQueryHandler> logger,
    IAppDbContext context) : IRequestHandler<GetPatientOdontogramQuery, Result<PatientOdontogramDto>>
{
    public async Task<Result<PatientOdontogramDto>> Handle(GetPatientOdontogramQuery request, CancellationToken cancellationToken)
    {
        var patient = await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Odontogram retrieval failed. Patient with ID {PatientId} not found.", request.PatientId);

            return ApplicationErrors.PatientNotFound;
        }

        if (!await PatientAccess.CanAccessAsync(context, request.Scope, patient, cancellationToken))
        {
            logger.LogWarning("Patient {PatientId} is outside the caller's authorized scope.", request.PatientId);
            return ApplicationErrors.PatientAccessForbidden;
        }

        // Two separate AsNoTracking queries — no joins between the tooth state
        // rows and the treatment records; the UI groups history per tooth.
        var teeth = await context.PatientTeeth
            .AsNoTracking()
            .Where(t => t.PatientId == request.PatientId)
            .OrderBy(t => t.ToothNumber)
            .ToListAsync(cancellationToken);

        var records = await context.TreatmentRecords
            .AsNoTracking()
            .Where(r => r.PatientId == request.PatientId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new PatientOdontogramDto(
            patient.Id,
            teeth.ToSummaryDtos(),
            records.ToHistoryDtos());
    }
}