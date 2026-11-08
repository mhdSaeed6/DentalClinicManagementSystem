namespace DentalClinic.Infrastructure.Data.Configurations;

using DentalClinic.Domain.Labs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LabCaseConfiguration : IEntityTypeConfiguration<LabCase>
{
    public void Configure(EntityTypeBuilder<LabCase> builder)
    {
        builder.ToTable("LabCases");

        // 1. Primary Key
        builder.HasKey(l => l.Id);

        // 2. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(l => !l.IsDeleted);

        // 3. Money must never fall back to an implicit decimal precision; SQL
        //    Server would silently truncate values that exceed the default
        //    (18, 2) mapping instead of failing.
        builder.Property(l => l.Cost)
            .HasPrecision(18, 2)
            .IsRequired();

        // 4. Nullable on purpose: a lab case does not always target one tooth.
        builder.Property(l => l.ToothNumber)
            .IsRequired(false);

        // 5. Required free text with bounded length so the list stays readable.
        builder.Property(l => l.LabName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(l => l.CaseType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(l => l.Status)
            .IsRequired();

        // 6. A lab case usually survives soft deletion of its treatment record,
        //    so the record reference must not cascade.
        builder.HasOne(l => l.TreatmentRecord)
            .WithMany()
            .HasForeignKey(l => l.TreatmentRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        // 7. Foreign Key Indexes for Performance
        builder.HasIndex(l => l.PatientId);
        builder.HasIndex(l => l.DoctorId);
        builder.HasIndex(l => l.TreatmentRecordId);

        // 8. List queries filter by patient and status (P1.2-style workflows),
        //    and the overdue view scans open cases by their expected return.
        builder.HasIndex(l => new { l.PatientId, l.Status });
        builder.HasIndex(l => l.ExpectedReturnAtUtc);
    }
}