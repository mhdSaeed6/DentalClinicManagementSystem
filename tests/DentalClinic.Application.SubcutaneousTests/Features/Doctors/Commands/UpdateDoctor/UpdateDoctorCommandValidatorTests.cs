using DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.UpdateDoctor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateDoctorCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task UpdateDoctor_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        // Arrange
        var command = CreateValidCommand() with { DoctorId = Guid.Empty };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDoctor_ShouldFailValidation_WhenFirstNameIsEmpty(string firstName)
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = firstName };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDoctor_ShouldFailValidation_WhenFirstNameExceedsMaxLength()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = new string('A', 51) };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDoctor_ShouldFailValidation_WhenLastNameIsEmpty(string lastName)
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = lastName };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDoctor_ShouldFailValidation_WhenLastNameExceedsMaxLength()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = new string('A', 51) };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDoctor_ShouldFailValidation_WhenSpecializationIsEmpty(string specialization)
    {
        // Arrange
        var command = CreateValidCommand() with { Specialization = specialization };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDoctor_ShouldFailValidation_WhenSpecializationExceedsMaxLength()
    {
        // Arrange
        var command = CreateValidCommand() with { Specialization = new string('A', 101) };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDoctor_ShouldFailValidation_WhenGenderIsInvalidEnum()
    {
        // Arrange
        var command = CreateValidCommand() with { Gender = (Gender)999 };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private static UpdateDoctorCommand CreateValidCommand() => new(
        DoctorId: Guid.NewGuid(),
        FirstName: "Ahmad",
        LastName: "Ali",
        Specialization: "Pediatric Dentistry",
        ContactInfo: ContactInfo.Create("+963911111111", null, true, null).Value,
        Gender: Gender.Male);
}