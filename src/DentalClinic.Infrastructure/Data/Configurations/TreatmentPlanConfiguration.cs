namespace DentalClinic.Infrastructure.Data.Configurations;

using DentalClinic.Domain.TreatmentPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        // 1. Primary Key
        builder.ToTable("TreatmentPlans");
        builder.HasKey(p => p.Id);

        // 2. Global Query Filter (Soft Delete) — D-P5: the plan is never
        //    hard-deleted; RemoveByTag + Delete() keep history intact.
        builder.HasQueryFilter(p => !p.IsDeleted);

        // 3. Column lengths — title is the printable sheet headline.
        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        // 4. Foreign Keys (Restrict — plan removal happens by soft delete only)
        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Doctor)
            .WithMany()
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 5. Foreign Key Indexes for Performance (list is per patient / doctor)
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.DoctorId);
    }
}