using DentalClinic.Application.Features.Patients.Commands.CreatePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.CreatePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreatePatientCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_ShouldFailValidation_WhenFirstNameIsEmpty(string firstName)
    {
        var command = CreateValidCommand() with { FirstName = firstName };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreatePatient_ShouldFailValidation_WhenDateOfBirthIsInFuture()
    {
        var command = CreateValidCommand() with { DateOfBirth = DateTime.UtcNow.AddDays(1) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static CreatePatientCommand CreateValidCommand() => new(
        FirstName: "Ahmad",
        LastName: "Kareem",
        ContactInfo: ContactInfo.Create("+963911111111", null, true, null).Value,
        DateOfBirth: DateTime.UtcNow.AddYears(-25),
        Gender: Gender.Male,
        MedicalHistory: new MedicalHistory());
}