using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Appointments;

namespace DentalClinic.Application.Features.Appointments.Mappers;

public static class AppointmentMappingExtensions
{
    public static AppointmentDto ToDto(this Appointment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AppointmentDto(
            entity.Id,
            entity.PatientId,
            entity.DoctorId,
            entity.ServiceId,
            entity.ScheduledDateTime,
            entity.DurationInMinutes,
            entity.Status,
            entity.Notes);
    }

    public static List<AppointmentDto> ToDtos(this IEnumerable<Appointment> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
