using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.HasKey(il => il.Id).IsClustered();

        // No soft-delete query filter, matching Payment: a line is part of its
        // invoice's value collection and is removed outright when replaced.
        builder.Property(il => il.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(il => il.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne<Invoice>()
            .WithMany(i => i.Lines)
            .HasForeignKey(il => il.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}