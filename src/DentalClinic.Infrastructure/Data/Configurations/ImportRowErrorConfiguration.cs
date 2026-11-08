using DentalClinic.Domain.Imports;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class ImportRowErrorConfiguration : IEntityTypeConfiguration<ImportRowError>
{
    public void Configure(EntityTypeBuilder<ImportRowError> builder)
    {
        builder.ToTable("ImportRowErrors");

        builder.HasKey(error => error.Id);

        // Match the ImportJob soft-delete filter: previewing a retired job's
        // diagnostics through the navigation must not resurrect them.
        builder.HasQueryFilter(error => !error.ImportJob!.IsDeleted);

        builder.Property(error => error.RowNumber)
            .IsRequired();

        builder.Property(error => error.Column)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(error => error.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(error => error.Value)
            .HasMaxLength(500);

        builder.HasIndex(error => new { error.ImportJobId, error.RowNumber });
    }
}