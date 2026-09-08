using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatientById;

public class GetPatientByIdQueryHandler(
    ILogger<GetPatientByIdQueryHandler> logger,
    IAppDbContext context) : IRequestHandler<GetPatientByIdQuery, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Patient retrieval failed. Patient with ID {PatientId} not found.", request.PatientId);

            return Error.NotFound(
                code: "Patient.NotFound",
                description: $"Patient with Id {request.PatientId} not found.");
        }

        return patient.ToDto();
    }
}