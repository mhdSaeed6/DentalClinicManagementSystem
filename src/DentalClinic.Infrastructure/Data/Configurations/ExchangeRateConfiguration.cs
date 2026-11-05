using DentalClinic.Domain.Clinics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.HasKey(r => r.Id).IsClustered();

        builder.Property(r => r.FromCurrency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(r => r.ToCurrency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        // Six decimals: enough to keep small-value currencies (e.g. 1 USD =
        // 0.000001 X) meaningful without pretending to be a trading system.
        builder.Property(r => r.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(r => r.EffectiveFromUtc)
            .IsRequired();

        // The lookup is always "this pair, newest first"; without the index it
        // is a table scan on every invoice print.
        builder.HasIndex(r => new { r.FromCurrency, r.ToCurrency, r.EffectiveFromUtc });

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}