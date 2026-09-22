using DentalClinic.Application.Features.Patients.Commands.RemovePatient;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Commands.RemovePatient;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemovePatientCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task RemovePatient_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        var command = new RemovePatientCommand(Guid.Empty);
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }
}