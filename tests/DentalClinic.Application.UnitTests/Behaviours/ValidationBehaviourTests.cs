using DentalClinic.Application.Common.Behaviours;
using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Mappers;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Tests.Common.Appointments;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

using NSubstitute;

using Xunit;

namespace DentalClinic.Application.UnitTests.Behaviours;

public class ValidationBehaviourTests
{
    private readonly ValidationBehaviour<CreateAppointmentCommand, Result<AppointmentDto>> _validationBehavior;
    private readonly IValidator<CreateAppointmentCommand> _mockValidator;
    private readonly RequestHandlerDelegate<Result<AppointmentDto>> _mockNextBehavior;

    public ValidationBehaviourTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<Result<AppointmentDto>>>();
        _mockValidator = Substitute.For<IValidator<CreateAppointmentCommand>>();

        _validationBehavior = new ValidationBehaviour<CreateAppointmentCommand, Result<AppointmentDto>>(_mockValidator);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsValid_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createAppointmentCommand = AppointmentCommandFactory.CreateCreateAppointmentCommand();
        var appointmentResponse = AppointmentFactory.CreateAppointment().Value.ToDto();

        _mockValidator
            .ValidateAsync(createAppointmentCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockNextBehavior.Invoke().Returns(appointmentResponse);

        // Act
        var result = await _validationBehavior.Handle(createAppointmentCommand, _mockNextBehavior, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(appointmentResponse, result.Value);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
    {
        // Arrange
        var createAppointmentCommand = AppointmentCommandFactory.CreateCreateAppointmentCommand();

        List<ValidationFailure> validationFailures = [new(propertyName: "property1", errorMessage: "property1 is invalid")];

        _mockValidator
            .ValidateAsync(createAppointmentCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _validationBehavior.Handle(createAppointmentCommand, _mockNextBehavior, default);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("property1", result.TopError.Code);
        Assert.Equal("property1 is invalid", result.TopError.Description);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenNoValidator_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createAppointmentCommand = AppointmentCommandFactory.CreateCreateAppointmentCommand();

        IValidator<CreateAppointmentCommand> nullValidator = null!;
        var validationBehavior = new ValidationBehaviour<CreateAppointmentCommand, Result<AppointmentDto>>(nullValidator);

        var appointmentResponse = AppointmentFactory.CreateAppointment().Value.ToDto();
        _mockNextBehavior.Invoke().Returns(appointmentResponse);

        // Act
        var result = await validationBehavior.Handle(createAppointmentCommand, _mockNextBehavior, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(appointmentResponse, result.Value);
    }
}