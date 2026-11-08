using DentalClinic.Domain.Patients;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class PatientImageConfiguration : IEntityTypeConfiguration<PatientImage>
{
    public void Configure(EntityTypeBuilder<PatientImage> builder)
    {
        builder.ToTable("PatientImages");

        // 1. Primary Key + Soft Delete Filter
        builder.HasKey(i => i.Id);
        builder.HasQueryFilter(i => !i.IsDeleted);

        // 2. Metadata
        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.UploadedBy)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(i => i.CapturedAtUtc)
            .IsRequired();

        builder.Property(i => i.ToothNumber)
            .IsRequired(false);

        // 3. Relationships. The patient is soft-deleted, never hard-deleted, so
        //    images must not cascade; the same applies to a linked treatment.
        builder.HasOne(i => i.Patient)
            .WithMany()
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TreatmentRecord)
            .WithMany()
            .HasForeignKey(i => i.TreatmentRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. The gallery is always read per patient, optionally by kind.
        builder.HasIndex(i => i.PatientId);
        builder.HasIndex(i => new { i.PatientId, i.Kind });
        builder.HasIndex(i => i.TreatmentRecordId);
    }
}