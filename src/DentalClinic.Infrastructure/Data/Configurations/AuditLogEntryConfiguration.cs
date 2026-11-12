using DentalClinic.Domain.Auditing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");

        // 1. Primary Key
        builder.HasKey(a => a.Id);

        // 2. Record identity
        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Identity ids are 450 chars (AspNetUsers.Id), so the actor column
        // matches that instead of the default nvarchar(max).
        builder.Property(a => a.UserId)
            .HasMaxLength(450);

        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        // 3. JSON snapshots are unbounded on purpose: the set of properties on
        //    an aggregate changes over time and truncating the history would
        //    silently lose the values the audit exists to preserve.
        builder.Property(a => a.OldValues)
            .IsRequired(false);

        builder.Property(a => a.NewValues)
            .IsRequired(false);

        // 4. The history view always filters by the record it belongs to and
        //    reads newest-first; a separate timestamp index serves the
        //    cross-record admin listing.
        builder.HasIndex(a => new { a.EntityName, a.EntityId, a.TimestampUtc });
        builder.HasIndex(a => a.TimestampUtc);

        // Deliberately no soft-delete query filter: the audit trail is
        // append-only and must never disappear with the record it describes.
    }
}
