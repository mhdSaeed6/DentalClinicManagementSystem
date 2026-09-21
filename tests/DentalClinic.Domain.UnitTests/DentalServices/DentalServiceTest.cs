using DentalClinic.Domain.Services;
using DentalClinic.Tests.Common.DentalServices;

namespace DentalClinic.Domain.UnitTests.DentalServices;

public class DentalServiceTest
{
    [Fact]
    public void CreateDentalService_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var name = "Teeth Cleaning";
        var description = "A thorough cleaning of the teeth.";
        var price = 100.0m;

        // Act
        var result = DentalServiceFactory.CreateDentalService(name, description, price);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var dentalService = result.Value;
        Assert.Equal(name, dentalService.Name);
        Assert.Equal(description, dentalService.Description);
        Assert.Equal(price, dentalService.Price);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDentalService_ShouldReturnError_WhenNameIsEmpty(string? invalidName)
    {
        // Act
        var result = DentalServiceFactory.CreateDentalService(name: invalidName!);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ServiceErrors.NameRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-25.0)]
    [InlineData(-50.0)]
    public void CreateDentalService_ShouldReturnError_WhenPriceISNegative(decimal invalidPrice)
    {
        // Act
        var result = DentalServiceFactory.CreateDentalService(price: invalidPrice);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ServiceErrors.InvalidPrice.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateDentalService_ShouldReturnSuccess_WhenPriceIsZero()
    {
        // Act
        var result = DentalServiceFactory.CreateDentalService(price: 0.0m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0.0m, result.Value.Price);
    }

    [Fact]
    public void UpdateDentalService_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var service = DentalServiceFactory.CreateDentalService().Value!;
        var newName = "Teeth Whitening";
        var newDescription = "Professional laser whitening.";
        var newPrice = 250.0m;

        // Act
        var result = service.Update(newName, newDescription, newPrice);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newName, service.Name);
        Assert.Equal(newDescription, service.Description);
        Assert.Equal(newPrice, service.Price);
    }
}