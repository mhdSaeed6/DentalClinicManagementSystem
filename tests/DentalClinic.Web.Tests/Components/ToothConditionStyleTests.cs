using DentalClinic.Web.Api;
using DentalClinic.Web.Components.Shared;

namespace DentalClinic.Web.Tests.Components;

public class ToothConditionStyleTests
{
    [Fact]
    public void EveryConditionHasDistinctCssSuffix()
    {
        var suffixes = Enum.GetValues<ToothCondition>()
            .Select(ToothConditionStyle.CssSuffix)
            .ToList();

        Assert.Equal(Enum.GetValues<ToothCondition>().Length, suffixes.Distinct().Count());
        Assert.DoesNotContain("unknown", suffixes);
    }

    [Theory]
    [InlineData(ToothCondition.Healthy, "healthy")]
    [InlineData(ToothCondition.Caries, "caries")]
    [InlineData(ToothCondition.Filled, "filled")]
    [InlineData(ToothCondition.Crown, "crown")]
    [InlineData(ToothCondition.Bridge, "bridge")]
    [InlineData(ToothCondition.Implant, "implant")]
    [InlineData(ToothCondition.Extracted, "extracted")]
    [InlineData(ToothCondition.Missing, "missing")]
    public void EveryConditionMapsToExpectedSuffix(ToothCondition condition, string suffix)
    {
        Assert.Equal(suffix, ToothConditionStyle.CssSuffix(condition));
    }

    [Theory]
    [InlineData(ToothCondition.Healthy, "Healthy", "سليم")]
    [InlineData(ToothCondition.Caries, "Caries", "تسوس")]
    [InlineData(ToothCondition.Filled, "Filled", "محشو")]
    [InlineData(ToothCondition.Crown, "Crown", "تاج")]
    [InlineData(ToothCondition.Bridge, "Bridge", "جسر")]
    [InlineData(ToothCondition.Implant, "Implant", "زرعة")]
    [InlineData(ToothCondition.Extracted, "Extracted", "مخلوع")]
    [InlineData(ToothCondition.Missing, "Missing", "مفقود")]
    public void EveryConditionHasEnglishAndArabicLabels(ToothCondition condition, string english, string arabic)
    {
        Assert.Equal(english, ToothConditionStyle.Label(condition, false));
        Assert.Equal(arabic, ToothConditionStyle.Label(condition, true));
    }
}
