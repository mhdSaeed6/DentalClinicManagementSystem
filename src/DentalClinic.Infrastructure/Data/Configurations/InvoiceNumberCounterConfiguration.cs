using DentalClinic.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class InvoiceNumberCounterConfiguration : IEntityTypeConfiguration<InvoiceNumberCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberCounter> builder)
    {
        builder.ToTable("InvoiceNumberCounters");

        // The year is the natural key: one counter per numbering year, so a new
        // year starts at 0001 without a reset job.
        builder.HasKey(c => c.Year);

        builder.Property(c => c.Year)
            .ValueGeneratedNever();

        builder.Property(c => c.LastValue)
            .IsRequired();

        // The value can only be raised by the generator; this keeps a negative
        // or hand-edited row from producing a nonsensical number.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InvoiceNumberCounters_LastValue_NonNegative",
            "[LastValue] >= 0"));
    }
}