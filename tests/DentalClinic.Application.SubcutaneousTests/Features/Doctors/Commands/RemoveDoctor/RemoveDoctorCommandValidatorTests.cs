using DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.RemoveDoctor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveDoctorCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task RemoveDoctor_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        // Arrange
        var command = new RemoveDoctorCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}