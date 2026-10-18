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

        // 4. Foreign Key Indexes for Performance
        builder.HasIndex(t => t.PatientId);
        builder.HasIndex(t => t.DoctorId);
        builder.HasIndex(t => t.AppointmentId);
    }
}