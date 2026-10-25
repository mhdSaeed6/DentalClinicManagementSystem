using DentalClinic.Domain.Common.ValueObjects;

namespace DentalClinic.Domain.UnitTests.ValueObjects;

public class FdiToothTests
{
    [Theory]
    [InlineData(11)] // حدود الربع الأول السفلي
    [InlineData(18)] // حدود الربع الأول العلوي
    [InlineData(21)]
    [InlineData(28)]
    [InlineData(31)]
    [InlineData(38)]
    [InlineData(41)]
    [InlineData(48)]
    [InlineData(51)] // حدود الأسنان اللبنية
    [InlineData(55)]
    [InlineData(61)]
    [InlineData(65)]
    [InlineData(71)]
    [InlineData(75)]
    [InlineData(81)]
    [InlineData(85)]
    public void IsValidToothNumber_ShouldReturnTrue_WhenToothNumberIsAtBoundary(int toothNumber)
    {
        // Act
        bool isValid = FdiTooth.IsValidToothNumber(toothNumber);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(10)] // خارج حدود كل الأرباع
    [InlineData(19)] // بين الربعين الأول والثاني
    [InlineData(20)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(39)]
    [InlineData(40)]
    [InlineData(49)]
    [InlineData(50)] // قبل الأرباع اللبنية
    [InlineData(56)] // بين الأرباع اللبنية
    [InlineData(60)]
    [InlineData(66)]
    [InlineData(70)]
    [InlineData(76)]
    [InlineData(80)]
    [InlineData(86)] // بعد الأرباع اللبنية
    [InlineData(0)]
    public void IsValidToothNumber_ShouldReturnFalse_WhenToothNumberIsOutsideBoundary(int toothNumber)
    {
        // Act
        bool isValid = FdiTooth.IsValidToothNumber(toothNumber);

        // Assert
        Assert.False(isValid);
    }
}