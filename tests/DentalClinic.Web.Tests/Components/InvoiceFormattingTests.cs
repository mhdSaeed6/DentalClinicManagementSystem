using DentalClinic.Web.Api;
using DentalClinic.Web.Components;

namespace DentalClinic.Web.Tests.Components;

public sealed class InvoiceFormattingTests
{
    [Fact]
    public void NumberLabel_IssuedInvoice_ReturnsTheStoredNumber()
    {
        var invoice = Invoice("INV-2026-0042", InvoiceLifecycleStatus.Issued);

        Assert.Equal("INV-2026-0042", InvoiceFormatting.NumberLabel(invoice));
    }

    [Fact]
    public void NumberLabel_Draft_ReturnsAShortIdAndNeverAFabricatedNumber()
    {
        var invoice = Invoice(invoiceNumber: null, InvoiceLifecycleStatus.Draft);

        var label = InvoiceFormatting.NumberLabel(invoice);

        Assert.Equal($"#{invoice.Id.ToString()[..8]}", label);
        Assert.DoesNotContain("INV-", label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NumberLabel_BlankNumber_IsTreatedAsUnnumbered(string invoiceNumber)
    {
        var invoice = Invoice(invoiceNumber, InvoiceLifecycleStatus.Issued);

        Assert.StartsWith("#", InvoiceFormatting.NumberLabel(invoice));
    }

    [Fact]
    public void NumberLabel_TrimsSurroundingWhitespace()
    {
        var invoice = Invoice("  INV-2026-0007  ", InvoiceLifecycleStatus.Issued);

        Assert.Equal("INV-2026-0007", InvoiceFormatting.NumberLabel(invoice));
    }

    [Fact]
    public void NumberLabel_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => InvoiceFormatting.NumberLabel(null!));
    }

    [Theory]
    [InlineData(InvoiceLifecycleStatus.Issued, true)]
    [InlineData(InvoiceLifecycleStatus.Draft, false)]
    public void IsPrintable_OnlyIssuedInvoicesAreRealBills(
        InvoiceLifecycleStatus lifecycleStatus,
        bool expected)
    {
        var invoice = Invoice("INV-2026-0001", lifecycleStatus);

        Assert.Equal(expected, InvoiceFormatting.IsPrintable(invoice));
    }

    private static InvoiceDto Invoice(string? invoiceNumber, InvoiceLifecycleStatus lifecycleStatus) =>
        new()
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            PatientId = Guid.NewGuid(),
            PatientName = "Test Patient",
            TotalAmount = 100m,
            LifecycleStatus = lifecycleStatus,
        };
}