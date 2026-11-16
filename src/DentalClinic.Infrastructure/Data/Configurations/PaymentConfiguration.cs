using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        // Global Query Filter (Soft Delete) — a payment is financial history and
        // must never reappear in queries once it has been voided.
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // تُخزَّن الطريقة كنص (كما يفعل Invoice.Status) لتبقى مقروءة في قاعدة
        // البيانات. القيمة الافتراضية "Cash" هي ما تحصل عليه الصفوف الموجودة،
        // وهو نفس افتراضي الكيان، فلا تتغيّر أي دفعة قديمة. الـ Sentinel يمنع
        // EF من إرسال القيمة الافتراضية للعمود عند الدفع نقداً (وبدونه يحذّر
        // EF في كل إقلاع بأن CLR default هو 0 وليس Cash).
        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentMethod.Cash)
            .HasSentinel(PaymentMethod.Cash)
            .IsRequired();

        builder.Property(p => p.Reference)
            .HasMaxLength(Payment.MaxReferenceLength);

        builder.HasIndex(p => p.InvoiceId);
    }
}