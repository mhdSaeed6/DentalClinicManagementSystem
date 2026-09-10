using DentalClinic.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id).IsClustered();

        // 1. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(a => !a.IsDeleted);

        // 2. Properties Setup
        builder.Property(a => a.ScheduledDateTime)
            .IsRequired();

        builder.Property(a => a.DurationInMinutes)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        // 3. Relationships (Foreign Keys)
        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Performance Indexes (مهمة جداً لجدول المواعيد)
        builder.HasIndex(a => new { a.DoctorId, a.ScheduledDateTime });
        builder.HasIndex(a => new { a.PatientId, a.ScheduledDateTime });
        builder.HasIndex(a => a.Status);
    }
}