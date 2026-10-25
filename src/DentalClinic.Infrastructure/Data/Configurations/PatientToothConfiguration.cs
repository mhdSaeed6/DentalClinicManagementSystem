using DentalClinic.Domain.Odontograms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class PatientToothConfiguration : IEntityTypeConfiguration<PatientTooth>
{
    public void Configure(EntityTypeBuilder<PatientTooth> builder)
    {
        builder.ToTable("PatientTeeth");

        builder.HasKey(pt => pt.Id).IsClustered();

        // 1. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(pt => !pt.IsDeleted);

        // 2. Properties Setup
        builder.Property(pt => pt.ToothNumber)
            .IsRequired();

        // الحالة تُخزَّن كرقم صحيح لأن القيم مضافة فقط (append-only)؛ لا تُحوَّل إلى نص.
        // Enums are stored as ints deliberately — values are append-only, so renames stay cheap.
        builder.Property(pt => pt.Condition)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(pt => pt.Surface)
            .HasConversion<int>()
            .IsRequired(false);

        builder.Property(pt => pt.Notes)
            .HasMaxLength(500);

        // 3. Relationships (Foreign Keys)
        builder.HasOne(pt => pt.Patient)
            .WithMany()
            .HasForeignKey(pt => pt.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Performance Indexes
        builder.HasIndex(pt => pt.PatientId);

        // صف واحد حي على الأكثر لكل (مريض، سن)؛ الصفوف المحذوفة ناعماً لا تمنع إعادة إضافة السن.
        // One live row at most per (patient, tooth): the filter keeps soft-deleted rows
        // out of the unique index, so they never block re-adding the same tooth.
        builder.HasIndex(pt => new { pt.PatientId, pt.ToothNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}