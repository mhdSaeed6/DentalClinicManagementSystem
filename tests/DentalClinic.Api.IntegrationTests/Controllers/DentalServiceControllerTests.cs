using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.DentalServices;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class DentalServiceControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/dentalservice
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDentalServices_WithValidPagination_ShouldReturnPaginatedList()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/dentalservice?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<DentalServiceDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetDentalServices_WithInvalidPagination_ShouldReturnBadRequest(int pageNumber, int pageSize)
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync($"/api/v1.0/dentalservice?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDentalServices_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/dentalservice?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. GET /api/v1.0/dentalservice/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDentalServiceById_WithValidId_ShouldReturnService()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var service = DentalServiceTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.DentalServices.Add(service);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync($"/api/v1.0/dentalservice/{service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DentalServiceDto>();
        Assert.NotNull(result);
        Assert.Equal(service.Id, result!.Id);
    }

    [Fact]
    public async Task GetDentalServiceById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/dentalservice/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDentalServiceById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var serviceId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1.0/dentalservice/{serviceId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. POST /api/v1.0/dentalservice
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateDentalService_WithValidRequest_ShouldCreateService()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateDentalServiceRequest
        {
            Name = "Teeth Whitening",
            Description = "Professional laser whitening",
            Price = 150.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/dentalservice", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdDto = await response.Content.ReadFromJsonAsync<DentalServiceDto>();
        Assert.NotNull(createdDto);
        Assert.Equal(request.Name, createdDto!.Name);
        Assert.Equal(request.Price, createdDto.Price);
    }

    [Fact]
    public async Task CreateDentalService_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateDentalServiceRequest
        {
            Name = string.Empty, // اسم فارغ غير مقبول
            Description = "Some desc",
            Price = -50.00m // سعر بالسالب غير مقبول
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/dentalservice", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDentalService_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateDentalServiceRequest
        {
            Name = "Teeth Cleaning",
            Description = "Basic cleaning",
            Price = 80.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/dentalservice", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 4. PUT /api/v1.0/dentalservice/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateDentalService_WithValidRequest_ShouldUpdateService()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var service = DentalServiceTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.DentalServices.Add(service);
            await context.SaveChangesAsync(default);
        });

        var request = new UpdateDentalServiceRequest
        {
            Name = "Updated Root Canal",
            Description = "Updated description",
            Price = 300.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/dentalservice/{service.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DentalServiceDto>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.Equal(request.Price, result.Price);
    }

    [Fact]
    public async Task UpdateDentalService_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();
        var request = new UpdateDentalServiceRequest
        {
            Name = "Root Canal",
            Description = "Description",
            Price = 200.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/dentalservice/{nonExistentId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 5. DELETE /api/v1.0/dentalservice/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteDentalService_WithValidId_ShouldDeleteService()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var service = DentalServiceTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.DentalServices.Add(service);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.DeleteAsync($"/api/v1.0/dentalservice/{service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDentalService_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1.0/dentalservice/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDentalService_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var serviceId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/v1.0/dentalservice/{serviceId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task EnsureUserExistsAsync()
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByIdAsync(TestUsers.Admin.Id);
        if (user is null)
        {
            await userManager.CreateAsync(TestUsers.Admin);
        }
    }
}