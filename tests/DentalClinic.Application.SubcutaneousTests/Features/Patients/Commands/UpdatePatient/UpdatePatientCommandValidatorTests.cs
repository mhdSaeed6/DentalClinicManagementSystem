using DentalClinic.Application.Features.Patients.Commands.UpdatePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.UpdatePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdatePatientCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task UpdatePatient_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        var command = new UpdatePatientCommand(
            PatientId: Guid.Empty,
            FirstName: "Saeed",
            LastName: "Mhd",
            ContactInfo: ContactInfo.Create("+963911111111", null, true, null).Value,
            DateOfBirth: DateTime.UtcNow.AddYears(-20),
            Gender: Gender.Male,
            MedicalHistory: new MedicalHistory());

        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }
}