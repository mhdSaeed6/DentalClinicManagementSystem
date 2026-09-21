using DentalClinic.Domain.Common.ValueObjects;

namespace DentalClinic.Domain.UnitTests.Common;

public class ContactInfoTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValidPrimaryPhoneOnly()
    {
        // Arrange
        var phone = "+963912345678";

        // Act
        var result = ContactInfo.Create(phone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(phone, result.Value.PrimaryPhone);
        Assert.Null(result.Value.SecondaryPhone);
        Assert.True(result.Value.HasWhatsAppOnPrimary);
        Assert.Null(result.Value.SocialMediaLink);
    }

    [Fact]
    public void Create_ShouldTrimInputsAndSetNull_WhenOptionalFieldsAreWhitespace()
    {
        // Arrange
        var primary = "  +963912345678  ";
        var secondary = "  +963987654321  ";
        var social = "  https://facebook.com/dr.somebody  ";

        // Act
        var result = ContactInfo.Create(
            primaryPhone: primary,
            secondaryPhone: secondary,
            hasWhatsAppOnPrimary: false,
            socialMediaLink: social);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("+963912345678", result.Value.PrimaryPhone);
        Assert.Equal("+963987654321", result.Value.SecondaryPhone);
        Assert.False(result.Value.HasWhatsAppOnPrimary);
        Assert.Equal("https://facebook.com/dr.somebody", result.Value.SocialMediaLink);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("12345")] // أقل من 7 أرقام
    [InlineData("1234567890123456")] // أكثر من 15 رقم
    [InlineData("abc1234567")] // يحتوي على أحرف
    public void Create_ShouldFail_WhenPrimaryPhoneIsInvalid(string? invalidPhone)
    {
        // Act
        var result = ContactInfo.Create(invalidPhone!);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ContactInfoErrors.InvalidPhoneNumber.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("invalid-phone")]
    public void Create_ShouldFail_WhenSecondaryPhoneIsInvalid(string invalidSecondaryPhone)
    {
        // Act
        var result = ContactInfo.Create(
            primaryPhone: "+963912345678",
            secondaryPhone: invalidSecondaryPhone);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ContactInfoErrors.InvalidPhoneNumber.Code, result.TopError.Code);
    }
}