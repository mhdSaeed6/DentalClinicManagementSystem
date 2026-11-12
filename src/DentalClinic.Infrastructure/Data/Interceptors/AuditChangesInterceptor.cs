using System.Globalization;
using System.Text.Json;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Auditing;
using DentalClinic.Domain.Common.Interfaces;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DentalClinic.Infrastructure.Data.Interceptors;

/// <summary>
/// Writes an <see cref="AuditLogEntry"/> for every create, update or delete of
/// an aggregate marked with <see cref="IRequireAuditTrail"/>.
///
/// The entries are added to the same <c>SaveChanges</c> call as the change
/// itself, so the history is committed atomically with the data and can never
/// describe a write that was rolled back.
///
/// Only the four sensitive aggregates are audited (Patient, Appointment,
/// Invoice, TreatmentRecord). The marker interface keeps every other table out
/// of the trail instead of filtering by name here.
/// </summary>
public class AuditChangesInterceptor(IUser user, TimeProvider dateTime) : SaveChangesInterceptor
{
    /// <summary>
    /// Bookkeeping columns are not business data: repeating them in every
    /// snapshot would bury the fields the clinic owner actually cares about.
    /// </summary>
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.Ordinal)
    {
        nameof(Entity.Id),
        nameof(AuditableEntity.CreatedAtUtc),
        nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.LastModifiedUtc),
        nameof(AuditableEntity.LastModifiedBy),
        nameof(AuditableEntity.IsDeleted),
        nameof(AuditableEntity.DeletedAtUtc),
        nameof(AuditableEntity.DeletedBy),
        nameof(Invoice.RowVersion),
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUser _user = user;
    private readonly TimeProvider _dateTime = dateTime;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Materialize first: adding audit rows below mutates the change tracker
        // we are iterating.
        var audited = context.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is IRequireAuditTrail)
            .ToList();

        if (audited.Count == 0)
        {
            return;
        }

        // A context can carry the same interceptor instance more than once (the
        // options are configured by every AddDbContext call, and EF composes
        // them). Nothing in the change tracker differs between those runs, so
        // without this guard one save would write the same history twice - and
        // so would a retry after a transient failure. An entity is tracked once,
        // so one pending entry per (entity, id, action) is exactly one change.
        var alreadyCaptured = context.ChangeTracker
            .Entries<AuditLogEntry>()
            .Where(entry => entry.State is EntityState.Added)
            .Select(entry => (entry.Entity.EntityName, entry.Entity.EntityId, entry.Entity.Action))
            .ToHashSet();

        var timestampUtc = _dateTime.GetUtcNow();
        var userId = _user.Id;
        var entries = new List<AuditLogEntry>();

        foreach (var entry in audited)
        {
            var action = ResolveAction(entry);

            if (action is null)
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;
            var entityId = GetEntityId(entry);

            if (!alreadyCaptured.Add((entityName, entityId, action.Value)))
            {
                continue;
            }

            var (oldValues, newValues) = CaptureSnapshots(context, entry, action.Value);

            entries.Add(AuditLogEntry.Create(
                entityName,
                entityId,
                action.Value,
                userId,
                timestampUtc,
                oldValues,
                newValues));
        }

        if (entries.Count > 0)
        {
            context.Set<AuditLogEntry>().AddRange(entries);
        }
    }

    private static AuditAction? ResolveAction(EntityEntry entry)
    {
        if (entry.State is EntityState.Added)
        {
            return AuditAction.Create;
        }

        if (entry.State is EntityState.Deleted)
        {
            return AuditAction.Delete;
        }

        if (entry.State is not EntityState.Modified)
        {
            return null;
        }

        // The soft-delete interceptor runs before this one and rewrites a
        // Deleted entry into a Modified one, so a "delete" is recognized by the
        // flag flipping rather than by EntityState.
        if (entry.Entity is ISoftDeletable softDeletable)
        {
            var isDeletedProperty = entry.Property(nameof(ISoftDeletable.IsDeleted));

            if (isDeletedProperty.IsModified && softDeletable.IsDeleted)
            {
                return AuditAction.Delete;
            }
        }

        // An owned value object (Patient.ContactInfo, Patient.MedicalHistory)
        // can change while the principal itself reports no modified scalar.
        return entry.Properties.Any(property => property.IsModified) || entry.HasChangedOwnedEntities()
            ? AuditAction.Update
            : null;
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null || primaryKey.Properties.Count == 0)
        {
            return Guid.Empty;
        }

        return entry.Property(primaryKey.Properties[0].Name).CurrentValue is Guid id
            ? id
            : Guid.Empty;
    }

    private static (string? OldValues, string? NewValues) CaptureSnapshots(
        DbContext context,
        EntityEntry entry,
        AuditAction action)
    {
        return action switch
        {
            AuditAction.Create => (null, Serialize(BuildSnapshot(context, entry, original: false))),
            AuditAction.Delete => (Serialize(BuildSnapshot(context, entry, original: true)), null),
            _ => (
                Serialize(BuildSnapshot(context, entry, original: true)),
                Serialize(BuildSnapshot(context, entry, original: false))),
        };
    }

    private static Dictionary<string, string?> BuildSnapshot(DbContext context, EntityEntry entry, bool original)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            if (!IsAuditedProperty(property.Metadata))
            {
                continue;
            }

            values[property.Metadata.Name] = FormatValue(original ? property.OriginalValue : property.CurrentValue);
        }

        foreach (var owned in OwnedEntriesOf(context, entry))
        {
            // A replaced owned instance is tracked twice: the old one is
            // Deleted and the replacement is Added. Each snapshot takes only
            // the half that belongs to it, so "before" never shows the new
            // phone number.
            if (original && owned.State is EntityState.Added)
            {
                continue;
            }

            if (!original && owned.State is EntityState.Deleted)
            {
                continue;
            }

            var prefix = owned.Metadata.FindOwnership()?.PrincipalToDependent?.Name
                         ?? owned.Metadata.ClrType.Name;

            foreach (var property in owned.Properties)
            {
                if (!IsAuditedProperty(property.Metadata))
                {
                    continue;
                }

                values[$"{prefix}.{property.Metadata.Name}"] =
                    FormatValue(original ? property.OriginalValue : property.CurrentValue);
            }
        }

        return values;
    }

    private static bool IsAuditedProperty(IProperty property) =>
        !property.IsShadowProperty() &&
        !property.IsPrimaryKey() &&
        !ExcludedProperties.Contains(property.Name);

    /// <summary>
    /// Finds the owned entries that belong to <paramref name="principal"/>,
    /// including ones already marked Deleted (a replaced value object), which
    /// are no longer reachable through the current navigation.
    /// </summary>
    private static IEnumerable<EntityEntry> OwnedEntriesOf(DbContext context, EntityEntry principal)
    {
        var principalKey = principal.Metadata.FindPrimaryKey();

        if (principalKey is null)
        {
            yield break;
        }

        var keyValues = principalKey.Properties
            .Select(property => principal.Property(property.Name).CurrentValue)
            .ToArray();

        foreach (var candidate in context.ChangeTracker.Entries())
        {
            if (!candidate.Metadata.IsOwned())
            {
                continue;
            }

            var ownership = candidate.Metadata.FindOwnership();

            if (ownership is null || ownership.PrincipalEntityType.ClrType != principal.Metadata.ClrType)
            {
                continue;
            }

            var belongsToPrincipal = true;

            for (var index = 0; index < ownership.Properties.Count; index++)
            {
                var foreignKey = candidate.Property(ownership.Properties[index].Name);

                if (foreignKey is null || !Equals(foreignKey.CurrentValue, keyValues[index]))
                {
                    belongsToPrincipal = false;
                    break;
                }
            }

            if (belongsToPrincipal)
            {
                yield return candidate;
            }
        }
    }

    private static string Serialize(Dictionary<string, string?> values) =>
        JsonSerializer.Serialize(values, JsonOptions);

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        string text => text,
        bool boolean => boolean ? "true" : "false",
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
        Enum enumValue => enumValue.ToString(),
        byte[] bytes => Convert.ToBase64String(bytes),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
