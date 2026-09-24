using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class DoctorControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/doctors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDoctors_WithValidPagination_ShouldReturnPaginatedList()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/doctors?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<DoctorDto>>();
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
    public async Task GetDoctors_WithInvalidPagination_ShouldReturnBadRequest(int pageNumber, int pageSize)
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync($"/api/v1.0/doctors?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctors_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/doctors?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. GET /api/v1.0/doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDoctorById_WithValidId_ShouldReturnDoctor()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var doctor = DoctorTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync($"/api/v1.0/doctors/{doctor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DoctorDto>();
        Assert.NotNull(result);
        Assert.Equal(doctor.Id, result!.Id);
    }

    [Fact]
    public async Task GetDoctorById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/doctors/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var doctorId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1.0/doctors/{doctorId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. POST /api/v1.0/doctors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateDoctor_WithValidRequest_ShouldCreateDoctor()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateDoctorRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Specialization = "Orthodontics",
            Gender = "Male",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1234567890",
                SecondaryPhone = "+0987654321",
                HasWhatsAppOnPrimary = true,
                SocialMediaLink = "https://linkedin.com/in/johndoe"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdDto = await response.Content.ReadFromJsonAsync<DoctorDto>();
        Assert.NotNull(createdDto);
        Assert.Equal(request.FirstName, createdDto!.FirstName);
        Assert.Equal(request.LastName, createdDto.LastName);
    }

    [Fact]
    public async Task CreateDoctor_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateDoctorRequest
        {
            FirstName = string.Empty, // اسم فارغ غير مقبول
            LastName = "Doe",
            Specialization = "Endodontics",
            Gender = "InvalidGenderValue",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "invalid-phone",
                SecondaryPhone = null,
                HasWhatsAppOnPrimary = false,
                SocialMediaLink = null
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDoctor_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateDoctorRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Specialization = "Periodontics",
            Gender = "Female",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1122334455",
                SecondaryPhone = null,
                HasWhatsAppOnPrimary = true,
                SocialMediaLink = null
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 4. PUT /api/v1.0/doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateDoctor_WithValidRequest_ShouldUpdateDoctor()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var doctor = DoctorTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        var request = new UpdateDoctorRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            Specialization = "Pediatric Dentistry",
            Gender = "Female",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+9988776655",
                SecondaryPhone = null,
                HasWhatsAppOnPrimary = false,
                SocialMediaLink = "https://instagram.com/updated_doctor"
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/doctors/{doctor.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DoctorDto>();
        Assert.NotNull(result);
        Assert.Equal(request.FirstName, result!.FirstName);
        Assert.Equal(request.LastName, result.LastName);
    }

    [Fact]
    public async Task UpdateDoctor_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();
        var request = new UpdateDoctorRequest
        {
            FirstName = "Alex",
            LastName = "Taylor",
            Specialization = "General Dentistry",
            Gender = "Male",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1234567890",
                SecondaryPhone = null,
                HasWhatsAppOnPrimary = true,
                SocialMediaLink = null
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/doctors/{nonExistentId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 5. DELETE /api/v1.0/doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteDoctor_WithValidId_ShouldDeleteDoctor()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var doctor = DoctorTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.DeleteAsync($"/api/v1.0/doctors/{doctor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoctor_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1.0/doctors/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoctor_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var doctorId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/v1.0/doctors/{doctorId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

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