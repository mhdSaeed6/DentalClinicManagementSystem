using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class PatientControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public PatientControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        // Authenticate client for protected endpoints
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1.0/patients
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPatients_WithDefaultPagination_ShouldReturnPaginatedList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/patients?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<PatientDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1.0/patients & GET /api/v1.0/patients/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreatePatient_WithValidRequest_ShouldCreateAndReturnPatient()
    {
        // Arrange
        var request = CreateValidPatientRequest("John", "Doe", "+1234567890");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/patients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(createdPatient);
        Assert.Equal(request.FirstName, createdPatient.FirstName);
        Assert.Equal(request.LastName, createdPatient.LastName);

        // Verify with GetById
        var getResponse = await _client.GetAsync($"/api/v1.0/patients/{createdPatient.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetPatientById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/patients/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1.0/patients/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePatient_WithValidData_ShouldUpdateAndReturnPatient()
    {
        // Arrange - Create a patient first
        var createRequest = CreateValidPatientRequest("Alice", "Smith", "+1122334455");
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var updateRequest = new UpdatePatientRequest
        {
            FirstName = "AliceUpdated",
            LastName = "SmithUpdated",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1122334455"
            },
            DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasDiabetes = true,
                HasHypertension = false,
                HasHeartDisease = false,
                Allergies = "Penicillin allergy",
                Notes = "Updated notes"
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/patients/{createdPatient!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedPatient = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(updatedPatient);
        Assert.Equal("AliceUpdated", updatedPatient.FirstName);
        Assert.Equal("SmithUpdated", updatedPatient.LastName);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1.0/patients/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemovePatient_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var createRequest = CreateValidPatientRequest("Mark", "Zucker", "+9988776655");
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1.0/patients/{createdPatient!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify patient is removed
        var getResponse = await _client.GetAsync($"/api/v1.0/patients/{createdPatient.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // Helper method
    private static CreatePatientRequest CreateValidPatientRequest(string firstName, string lastName, string phone)
    {
        return new CreatePatientRequest
        {
            FirstName = firstName,
            LastName = lastName,
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = phone
            },
            DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasDiabetes = true,
                HasHypertension = false,
                HasHeartDisease = false,
                Allergies = "Penicillin allergy",
                Notes = "Updated notes"
            }
        };
    }
}