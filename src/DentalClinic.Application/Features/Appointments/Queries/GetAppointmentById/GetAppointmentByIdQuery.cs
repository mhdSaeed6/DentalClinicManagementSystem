using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid AppointmentId, DoctorAccessScope Scope) : ICachedQuery<Result<AppointmentDto>>
{
    public string CacheKey => $"appointment-{AppointmentId}-scope-{Scope.IsRestricted}-{Scope.DoctorId}";

    public string[] Tags => ["appointment"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
