using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Auditing;

/// <summary>
/// Append-only record of one change made to a sensitive aggregate: who changed
/// which record, when, and what the values were before and after.
///
/// Entries are written by the infrastructure interceptor in the same
/// <c>SaveChanges</c> as the change itself, so the history can never drift from
/// the data. <see cref="OldValues"/> and <see cref="NewValues"/> are JSON
/// objects of property name to value; they are null for a create (no previous
/// state) and a delete (no resulting state) respectively.
/// </summary>
public sealed class AuditLogEntry : Entity
{
    private AuditLogEntry()
    { }

    private AuditLogEntry(
        Guid id,
        string entityName,
        Guid entityId,
        AuditAction action,
        string? userId,
        DateTimeOffset timestampUtc,
        string? oldValues,
        string? newValues)
        : base(id)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        TimestampUtc = timestampUtc;
        OldValues = oldValues;
        NewValues = newValues;
    }

    /// <summary>CLR name of the audited aggregate, e.g. <c>Patient</c>.</summary>
    public string EntityName { get; private set; } = default!;

    /// <summary>Primary key of the audited record.</summary>
    public Guid EntityId { get; private set; }

    public AuditAction Action { get; private set; }

    /// <summary>Identity account that performed the change. Null when the change
    /// was made outside an authenticated request (seeding, background job).</summary>
    public string? UserId { get; private set; }

    public DateTimeOffset TimestampUtc { get; private set; }

    /// <summary>JSON snapshot before the change; null on create.</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON snapshot after the change; null on delete.</summary>
    public string? NewValues { get; private set; }

    public static AuditLogEntry Create(
        string entityName,
        Guid entityId,
        AuditAction action,
        string? userId,
        DateTimeOffset timestampUtc,
        string? oldValues = null,
        string? newValues = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new AuditLogEntry(
            Guid.NewGuid(),
            entityName.Trim(),
            entityId,
            action,
            string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            timestampUtc,
            oldValues,
            newValues);
    }
}
