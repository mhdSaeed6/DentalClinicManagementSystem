using DentalClinic.Web.Components;

namespace DentalClinic.Web.Tests.Components;

public class ClinicFormattingTests
{
    [Fact]
    public void ToothLabel_ShouldReturnDash_WhenToothNumberIsNull()
    {
        // S8: a treatment recorded without a tooth shows an em dash, not a fake tooth.
        Assert.Equal("—", ClinicFormatting.ToothLabel(null));
    }

    [Theory]
    [InlineData(26, "UL·6")]
    [InlineData(36, "LL·6")]
    [InlineData(11, "UR·1")]
    public void ToothLabel_ShouldSegmentQuadrants_TheSameForNullableAndPlainValues(int toothNumber, string expected)
    {
        Assert.Equal(expected, ClinicFormatting.ToothLabel(toothNumber));
        Assert.Equal(expected, ClinicFormatting.ToothLabel((int?)toothNumber));
    }
}