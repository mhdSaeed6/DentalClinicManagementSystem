using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Domain.Auditing;

namespace DentalClinic.Application.Features.Auditing.Mappers;

public static class AuditLogMappingExtensions
{
    /// <summary>
    /// Projects a stored entry. The clinic timezone is required rather than
    /// defaulted: a timestamp shown in the server's zone is a wrong timestamp.
    /// </summary>
    public static AuditLogEntryDto ToDto(this AuditLogEntry entry, TimeZoneInfo timeZone) =>
        new(
            entry.Id,
            entry.EntityName,
            entry.EntityId,
            entry.Action.ToString(),
            entry.UserId,
            entry.TimestampUtc,
            TimeZoneInfo.ConvertTime(entry.TimestampUtc, timeZone).DateTime,
            entry.OldValues,
            entry.NewValues);
}
