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

        // 2. Foreign Key Indexes for Performance
        builder.HasIndex(t => t.PatientId);
        builder.HasIndex(t => t.DoctorId);
        builder.HasIndex(t => t.AppointmentId);
    }
}