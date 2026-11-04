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

        builder.Navigation(i => i.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(i => i.LifecycleStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Identity of the bill. Null while the invoice is a draft, so the column
        // must stay nullable; uniqueness is enforced only for the rows that
        // actually carry a number.
        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(Invoice.MaxInvoiceNumberLength);

        builder.Property(i => i.HasManualAdjustments)
            .HasColumnName("HasManualAdjustments")
            .IsRequired();

        // Optimistic concurrency token. Without it two concurrent payment
        // requests both read the same PaidAmount, both pass the
        // "amount <= RemainingAmount" check in memory, and the last writer wins
        // — silently losing a payment or overpaying an invoice. EF now includes
        // RowVersion in the UPDATE's WHERE clause and throws DbUpdateConcurrencyException
        // on the loser instead.
        builder.Property(i => i.RowVersion)
            .IsRowVersion();

        builder.HasIndex(i => i.PatientId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.LifecycleStatus);

        // One bill per visit. The check in the handler is only a friendly
        // pre-flight; this index is what actually makes the rule true under
        // concurrency. Soft-deleted rows are excluded so a visit can be billed
        // again after a mistaken invoice is deleted.
        builder.HasIndex(i => i.AppointmentId)
            .IsUnique()
            .HasFilter("[AppointmentId] IS NOT NULL AND [IsDeleted] = 0");

        // No two invoices may ever share a number. Drafts have no number, so
        // they are excluded from the constraint rather than colliding on NULL.
        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique()
            .HasFilter("[InvoiceNumber] IS NOT NULL");
    }
}