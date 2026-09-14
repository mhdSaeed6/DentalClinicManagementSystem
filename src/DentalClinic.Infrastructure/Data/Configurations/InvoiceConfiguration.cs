// using DentalClinic.Domain.Invoices;
// using DentalClinic.Domain.Patients;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;

// namespace DentalClinic.Infrastructure.Data.Configurations;

// public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
// {
//     public void Configure(EntityTypeBuilder<Invoice> builder)
//     {
//         builder.ToTable("Invoices");

//         builder.HasKey(i => i.Id).IsClustered();

//         // 1. Global Query Filter (Soft Delete)
//         builder.HasQueryFilter(i => !i.IsDeleted);

//         // 2. Precision for Monetary Values
//         builder.Property(i => i.TotalAmount)
//             .HasPrecision(18, 2)
//             .IsRequired();

//         builder.Property(i => i.PaidAmount)
//             .HasPrecision(18, 2)
//             .IsRequired();

//         // Ignoring calculated read-only property
//         builder.Ignore(i => i.RemainingAmount);

//         // 3. Enum Storage (AsString)
//         builder.Property(i => i.Status)
//             .HasConversion<string>()
//             .HasMaxLength(20)
//             .IsRequired();

//         // 4. Relationships
//         builder.HasOne<Patient>()
//             .WithMany()
//             .HasForeignKey(i => i.PatientId)
//             .OnDelete(DeleteBehavior.Restrict);

//         // Navigation to Payments
//         builder.HasMany(i => i.Payments)
//             .WithOne()
//             .HasForeignKey(p => p.InvoiceId)
//             .OnDelete(DeleteBehavior.Cascade);

//         builder.Navigation(i => i.Payments)
//             .UsePropertyAccessMode(PropertyAccessMode.Field);

//         // 5. Performance Indexes
//         builder.HasIndex(i => i.PatientId);
//         builder.HasIndex(i => i.AppointmentId);
//         builder.HasIndex(i => i.Status);
//     }
// }

using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id).IsClustered();

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.PaidAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(i => i.RemainingAmount);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // علاقة 1-to-Many قياسية ونظيفة
        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => i.PatientId);
        builder.HasIndex(i => i.AppointmentId);
        builder.HasIndex(i => i.Status);
    }
}