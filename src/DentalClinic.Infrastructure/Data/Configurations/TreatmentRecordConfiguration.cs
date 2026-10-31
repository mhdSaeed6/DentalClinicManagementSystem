namespace DentalClinic.Infrastructure.Data.Configurations;

using DentalClinic.Domain.TreatmentRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TreatmentRecordConfiguration : IEntityTypeConfiguration<TreatmentRecord>
{
    public void Configure(EntityTypeBuilder<TreatmentRecord> builder)
    {
        builder.ToTable("TreatmentRecords");

        // 1. Primary Key
        builder.HasKey(t => t.Id);

        // 2. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(t => !t.IsDeleted);

        // 3. Money must never fall back to an implicit decimal precision; SQL
        //    Server would silently truncate values that exceed the default
        //    (18, 2) mapping instead of failing.
        builder.Property(t => t.Cost)
            .HasPrecision(18, 2)
            .IsRequired();

        // 4. S8: a treatment may be recorded without a specific tooth (e.g. a
        //    general consultation) — null means "not linked to a tooth". Kept
        //    explicit so a future convention change cannot silently flip it back
        //    to required.
        builder.Property(t => t.ToothNumber)
            .IsRequired(false);

        // 5. P1.1 — EstimatedCost is the plan-time estimate; never billed and
        //    always distinct from Cost (D-P2). Nullable: ad-hoc work has no
        //    estimate.
        builder.Property(t => t.EstimatedCost)
            .HasPrecision(18, 2)
            .IsRequired(false);

        // 6. P1.1 — session grouping for per-session totals and printing.
        builder.Property(t => t.SessionNumber)
            .HasDefaultValue(1);

        // 7. P1.1 — the plan reference is soft-deleted by plan soft delete
        //    only (D-P5); Restrict blocks accidental hard-deletion of a plan
        //    that still holds items.
        builder.HasOne(t => t.Plan)
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // 8. Foreign Key Indexes for Performance
        builder.HasIndex(t => t.PatientId);
        builder.HasIndex(t => t.DoctorId);
        builder.HasIndex(t => t.AppointmentId);
        builder.HasIndex(t => t.PlanId);

        // 9. List queries filter by patient then status (P1.2)
        builder.HasIndex(t => new { t.PatientId, t.Status });
    }
}