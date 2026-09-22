using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Mappers;
using DentalClinic.Domain.Services;
using DentalClinic.Tests.Common.DentalServices;

using Xunit;

namespace DentalClinic.Application.UnitTests.Mappers;

public class DentalServiceMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var dentalService = DentalServiceFactory.CreateDentalService().Value;

        // Act
        var dto = dentalService.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(dentalService.Id, dto.Id);
        Assert.Equal(dentalService.Name, dto.Name);
        Assert.Equal(dentalService.Description, dto.Description);
        Assert.Equal(dentalService.Price, dto.Price);
        Assert.Equal(dentalService.IsActive, dto.IsActive);
    }

    [Fact]
    public void ToDto_WhenEntityIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        DentalService dentalService = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dentalService.ToDto());
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var service1 = DentalServiceFactory.CreateDentalService().Value;
        var service2 = DentalServiceFactory.CreateDentalService().Value;
        var services = new List<DentalService> { service1, service2 };

        // Act
        var dtos = services.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(service1.Id, dtos[0].Id);
        Assert.Equal(service2.Id, dtos[1].Id);
    }

    [Fact]
    public void ToDtos_WhenListIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var services = new List<DentalService>();

        // Act
        var dtos = services.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
}