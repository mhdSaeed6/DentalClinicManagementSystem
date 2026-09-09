using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid AppointmentId) : ICachedQuery<Result<AppointmentDto>>
{
    public string CacheKey => $"appointment-{AppointmentId}";

    public string[] Tags => ["appointment"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
