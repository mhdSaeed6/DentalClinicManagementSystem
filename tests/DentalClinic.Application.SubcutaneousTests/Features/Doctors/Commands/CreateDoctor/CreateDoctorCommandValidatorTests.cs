using DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.CreateDoctor;

public class CreateDoctorCommandValidatorTests : IClassFixture<WebAppFactory>
{
    private readonly ISender _sender;

    public CreateDoctorCommandValidatorTests(WebAppFactory factory)
    {
        var scope = factory.Services.CreateScope();
        _sender = scope.ServiceProvider.GetRequiredService<ISender>();
    }

    [Fact]
    public async Task CreateDoctor_ShouldSucceed_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateDoctor_ShouldFailValidation_WhenFirstNameIsEmptyOrNull(string? firstName)
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = firstName! };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenFirstNameExceedsMaxLength()
    {
        // Arrange
        var longFirstName = new string('A', 51);
        var command = CreateValidCommand() with { FirstName = longFirstName };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateDoctor_ShouldFailValidation_WhenLastNameIsEmptyOrNull(string? lastName)
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = lastName! };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenLastNameExceedsMaxLength()
    {
        // Arrange
        var longLastName = new string('A', 51);
        var command = CreateValidCommand() with { LastName = longLastName };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateDoctor_ShouldFailValidation_WhenSpecializationIsEmptyOrNull(string? specialization)
    {
        // Arrange
        var command = CreateValidCommand() with { Specialization = specialization! };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenSpecializationExceedsMaxLength()
    {
        // Arrange
        var longSpecialization = new string('A', 101);
        var command = CreateValidCommand() with { Specialization = longSpecialization };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenGenderIsInvalidEnum()
    {
        // Arrange
        var invalidGender = (Gender)99;
        var command = CreateValidCommand() with { Gender = invalidGender };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenContactInfoIsNull()
    {
        // Arrange
        var command = CreateValidCommand() with { ContactInfo = null! };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("12345")] // Less than 7 digits
    [InlineData("1234567890123456")] // More than 15 digits
    [InlineData("abc1234567")] // Contains letters
    public async Task CreateDoctor_ShouldFailValidation_WhenPrimaryPhoneIsInvalid(string invalidPhone)
    {
        // Arrange
        var contactInfo = ContactInfo.Create(invalidPhone, "0987654321", true, "https://social.com").Value;
        var command = CreateValidCommand() with { ContactInfo = contactInfo };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenSecondaryPhoneIsInvalidFormat()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", "invalid_phone", true, "https://social.com").Value;
        var command = CreateValidCommand() with { ContactInfo = contactInfo };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDoctor_ShouldFailValidation_WhenSocialMediaLinkExceedsMaxLength()
    {
        // Arrange
        var longUrl = "https://social.com/" + new string('a', 201);
        var contactInfo = ContactInfo.Create("+963911111111", "+963922222222", true, longUrl).Value;
        var command = CreateValidCommand() with { ContactInfo = contactInfo };

        // Act
        var result = await _sender.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    // Helper Method لتوليد Command ممتلئ ببيانات صحيحة لسهولة التعديل عليه عبر C# Records `with`
    private static CreateDoctorCommand CreateValidCommand()
    {
        var contactInfo = ContactInfo.Create("+963911111111", "+963922222222", true, "https://facebook.com/doctor").Value;

        return new CreateDoctorCommand(
            FirstName: "John",
            LastName: "Doe",
            Specialization: "Orthodontics",
            ContactInfo: contactInfo,
            Gender: Gender.Male);
    }
}