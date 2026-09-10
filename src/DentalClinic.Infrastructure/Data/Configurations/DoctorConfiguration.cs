using DentalClinic.Domain.Doctors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id).IsClustered();

        // 1. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(d => !d.IsDeleted);

        // 2. Strongly-Typed Properties
        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Specialization)
            .IsRequired()
            .HasMaxLength(100);

        // 3. Enum Storage (AsString)
        builder.Property(d => d.Gender).HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true)
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

        // 5. Performance Indexes
        builder.HasIndex(d => new { d.LastName, d.FirstName });
        builder.HasIndex(d => d.IsActive);
    }
}