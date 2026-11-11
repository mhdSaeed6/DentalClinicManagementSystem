namespace DentalClinic.Application.Features.Auditing.Dtos;

/// <summary>
/// One audit-trail row. <see cref="OldValues"/> and <see cref="NewValues"/> are
/// JSON objects of property name to value; the client renders the diff.
///
/// Both timestamps are the same instant: <see cref="TimestampUtc"/> is the
/// unambiguous one (sorting, exports), <see cref="TimestampLocal"/> is already
/// converted to the clinic's timezone so the UI never formats in the server's
/// zone (AUDIT.md D-09).
/// </summary>
public sealed record AuditLogEntryDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    string Action,
    string? UserId,
    DateTimeOffset TimestampUtc,
    DateTime TimestampLocal,
    string? OldValues,
    string? NewValues);
