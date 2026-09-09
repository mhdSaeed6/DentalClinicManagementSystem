using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;

public class GetDoctorByIdQueryHandler(
    ILogger<GetDoctorByIdQueryHandler> logger,
    IAppDbContext context) : IRequestHandler<GetDoctorByIdQuery, Result<DoctorDto>>
{
    public async Task<Result<DoctorDto>> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (doctor is null)
        {
            logger.LogWarning("Doctor retrieval failed. Doctor with ID {DoctorId} not found.", request.DoctorId);

            return ApplicationErrors.DoctorNotFound;
        }

        return doctor.ToDto();
    }
}
