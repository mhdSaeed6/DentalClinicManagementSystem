using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointments;

public class GetAppointmentsQueryHandler(IAppDbContext context) : IRequestHandler<GetAppointmentsQuery, Result<PaginatedList<AppointmentDto>>>
{
    public async Task<Result<PaginatedList<AppointmentDto>>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Appointments.AsNoTracking();

        if (request.PatientId.HasValue)
        {
            query = query.Where(a => a.PatientId == request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);
        }

        if (request.ServiceId.HasValue)
        {
            query = query.Where(a => a.ServiceId == request.ServiceId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var appointments = await query
            .OrderByDescending(a => a.ScheduledDateTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<AppointmentDto>
        {
            Items = appointments.ToDtos(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
