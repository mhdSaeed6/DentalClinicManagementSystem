using DentalClinic.Domain.Clinics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class ClinicSettingConfiguration : IEntityTypeConfiguration<ClinicSetting>
{
    public void Configure(EntityTypeBuilder<ClinicSetting> builder)
    {
        builder.ToTable("ClinicSettings");

        // One row, id supplied by the domain: ValueGeneratedNever stops EF from
        // treating the well-known id as a temporary client value.
        builder.HasKey(c => c.Id).IsClustered();
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(c => c.NameArabic)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(400);

        builder.Property(c => c.Phone)
            .HasMaxLength(40);

        builder.Property(c => c.Address)
            .HasMaxLength(300);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.BaseCurrency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(c => c.SecondaryCurrency)
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(c => c.DefaultCountryCode)
            .HasMaxLength(6);

        // Deliberately no soft-delete filter: the singleton row is configuration,
        // not business data, and hiding it would make the app unconfigurable.
    }
}