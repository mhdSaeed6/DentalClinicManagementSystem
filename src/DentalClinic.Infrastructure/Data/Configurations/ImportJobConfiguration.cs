using DentalClinic.Domain.Imports;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");

        builder.HasKey(job => job.Id);

        // Import jobs are an audit trail and are never deleted from the UI;
        // the soft-delete filter keeps them consistent with every other
        // auditable aggregate if an administrator ever retires one.
        builder.HasQueryFilter(job => !job.IsDeleted);

        builder.Property(job => job.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(job => job.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(job => job.TotalRows)
            .IsRequired();

        builder.Property(job => job.ImportedRows)
            .IsRequired();

        builder.Property(job => job.RejectedRows)
            .IsRequired();

        builder.Property(job => job.ErrorReportPath)
            .HasMaxLength(500);

        builder.Property(job => job.ImportedBy)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasMany(job => job.Errors)
            .WithOne(error => error.ImportJob)
            .HasForeignKey(error => error.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(job => job.CreatedAtUtc);
        builder.HasIndex(job => job.Status);
        builder.HasIndex(job => job.ImportedBy);
    }
}