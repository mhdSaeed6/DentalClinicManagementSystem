using DentalClinic.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id).IsClustered();

        // 1. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(p => !p.IsDeleted);

        // 2. Strongly-Typed Properties
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        // 3. Enum Storage (AsString)
        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // 4. Value Object: ContactInfo
        builder.OwnsOne(d => d.ContactInfo, contactInfo =>
        {
            contactInfo.Property(c => c.PrimaryPhone)
                .HasColumnName("PrimaryPhone")
                .HasMaxLength(20)
                .IsRequired();

            contactInfo.Property(c => c.SecondaryPhone)
                .HasColumnName("SecondaryPhone")
                .HasMaxLength(20);

            contactInfo.Property(c => c.HasWhatsAppOnPrimary)
                .HasColumnName("HasWhatsAppOnPrimary")
                .HasDefaultValue(true)
                .IsRequired();

            contactInfo.Property(c => c.SocialMediaLink)
                .HasColumnName("SocialMediaLink")
                .HasMaxLength(300);
        });

        // 5. Value Object: MedicalHistory (Owned Entity / Complex Type)
        builder.OwnsOne(p => p.MedicalHistory, medicalHistory =>
        {
            medicalHistory.Property(m => m.HasDiabetes)
                .HasColumnName("HasDiabetes")
                .IsRequired();

            medicalHistory.Property(m => m.HasHypertension)
                .HasColumnName("HasHypertension")
                .IsRequired();

            medicalHistory.Property(m => m.HasHeartDisease)
                .HasColumnName("HasHeartDisease")
                .IsRequired();

            medicalHistory.Property(m => m.Allergies)
                .HasColumnName("Allergies")
                .HasMaxLength(500);

            medicalHistory.Property(m => m.Notes)
                .HasColumnName("MedicalNotes")
                .HasMaxLength(1000);
        });

        // 6. Performance Indexes
        builder.HasIndex(p => new { p.LastName, p.FirstName });
    }
}