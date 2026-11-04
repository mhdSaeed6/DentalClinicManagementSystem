using DentalClinic.Domain.Invoices;

namespace DentalClinic.Domain.UnitTests.Invoices;

public class InvoiceNumberTest
{
    [Fact]
    public void Format_ShouldBuildPrefixYearAndPaddedSequence()
    {
        var number = InvoiceNumber.Format("INV", 4, 2026, 42);

        Assert.Equal("INV-2026-0042", number);
    }

    [Fact]
    public void Format_FirstNumberOfTheYear_ShouldBePadded()
    {
        Assert.Equal("INV-2026-0001", InvoiceNumber.Format("INV", 4, 2026, 1));
    }

    [Fact]
    public void Format_PastThePadding_ShouldGrowRatherThanWrap()
    {
        // Wrapping here would reissue an existing number, which is worse than a
        // five-digit sequence.
        Assert.Equal("INV-2026-10000", InvoiceNumber.Format("INV", 4, 2026, 10000));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_WithoutPrefix_ShouldFallBackToTheDefault(string? prefix)
    {
        Assert.Equal("INV-2026-0007", InvoiceNumber.Format(prefix, 4, 2026, 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Format_WithNonsensicalPadding_ShouldFallBackToTheDefault(int padding)
    {
        Assert.Equal("INV-2026-0007", InvoiceNumber.Format("INV", padding, 2026, 7));
    }

    [Fact]
    public void Format_ShouldTrimThePrefix()
    {
        Assert.Equal("CLINIC-2026-0003", InvoiceNumber.Format("  CLINIC  ", 4, 2026, 3));
    }

    [Fact]
    public void Format_WithNegativeSequence_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvoiceNumber.Format("INV", 4, 2026, -1));
    }
}