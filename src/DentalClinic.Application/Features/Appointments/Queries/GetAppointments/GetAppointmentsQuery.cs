using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointments;

public sealed record GetAppointmentsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? PatientId = null,
    Guid? DoctorId = null,
    Guid? ServiceId = null,
    AppointmentStatus? Status = null) : ICachedQuery<Result<PaginatedList<AppointmentDto>>>
{
    public string CacheKey => $"appointments-page-{PageNumber}-size-{PageSize}-patient-{PatientId?.ToString() ?? "all"}-doctor-{DoctorId?.ToString() ?? "all"}-service-{ServiceId?.ToString() ?? "all"}-status-{Status?.ToString() ?? "all"}";

    public string[] Tags => ["appointment"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
