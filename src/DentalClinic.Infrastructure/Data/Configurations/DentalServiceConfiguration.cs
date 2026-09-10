using DentalClinic.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class DentalServiceConfiguration : IEntityTypeConfiguration<DentalService>
{
    public void Configure(EntityTypeBuilder<DentalService> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(s => s.Id).IsClustered();

        // 1. Core Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // 2. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(s => !s.IsDeleted);

        // 3. Performance Indexes
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.IsActive);
    }
}