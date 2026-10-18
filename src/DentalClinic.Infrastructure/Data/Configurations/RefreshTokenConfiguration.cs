using DentalClinic.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id).IsClustered();

        // 1. Core Properties
        // Only the SHA-256 hash is persisted; SHA-256 hex is 64 chars.
        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasMaxLength(450); // متوافق مع طول PrimaryKey الافتراضي في ASP.NET Core Identity

        builder.Property(rt => rt.ExpiresOnUtc)
            .IsRequired();

        // 2. Global Query Filter (Soft Delete)
        builder.HasQueryFilter(rt => !rt.IsDeleted);

        // 3. Performance Indexes
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique();

        builder.HasIndex(rt => rt.UserId);
    }
}