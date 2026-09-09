using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler(
    ILogger<GetAppointmentByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentDto>>
{
    public async Task<Result<AppointmentDto>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            logger.LogWarning("Appointment retrieval failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        return appointment.ToDto();
    }
}
