using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Tests.Common.Security;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

/// <summary>
/// End-to-end smoke of the medical-alert fields: they round-trip through
/// create/update and the server computes and exposes the alert severity plus
/// a dashboard-ready count.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class PatientMedicalAlertControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public PatientMedicalAlertControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreatePatient_WithAlertFields_ShouldRoundTripAndComputeSeverity()
    {
        // Arrange — bleeding-related history is critical on its own.
        var request = CreatePatientRequest("Ali", "Karim", h =>
        {
            h.HasBleedingDisorder = true;
            h.BloodPressureReading = "150/98";
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created!.MedicalHistory);
        Assert.True(created.MedicalHistory!.HasBleedingDisorder);
        Assert.Equal("150/98", created.MedicalHistory.BloodPressureReading);
        Assert.Equal(2, created.MedicalHistory.AlertSeverity);

        // The stored history survives a fresh read too.
        var fetched = await (await _client.GetAsync($"/api/v1.0/patients/{created.Id}"))
            .Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(fetched!.MedicalHistory);
        Assert.Equal(2, fetched.MedicalHistory!.AlertSeverity);
        Assert.True(fetched.MedicalHistory.HasBleedingDisorder);
    }

    [Fact]
    public async Task CreatePatient_WithPregnancy_ShouldRoundTripWeeksAndWarn()
    {
        // Arrange — pregnancy with a normal blood pressure is only a warning.
        var request = CreatePatientRequest("Dana", "Ward", h =>
        {
            h.IsPregnant = true;
            h.PregnancyWeeks = 32;
            h.BloodPressureReading = "118/76";
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created!.MedicalHistory);
        Assert.True(created.MedicalHistory!.IsPregnant);
        Assert.Equal(32, created.MedicalHistory.PregnancyWeeks);
        Assert.Equal("118/76", created.MedicalHistory.BloodPressureReading);
        Assert.Equal(1, created.MedicalHistory.AlertSeverity);
    }

    [Fact]
    public async Task UpdatePatient_WithAlertFields_ShouldRoundTripThroughUpdate()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/patients", CreatePatientRequest("Hala", "Omar"));
        var created = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var update = new UpdatePatientRequest
        {
            FirstName = "Hala",
            LastName = "Omar",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+963944555999"
            },
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasAsthma = true,
                BloodPressureReading = "122/81",
                PregnancyWeeks = 8,
                IsPregnant = true,
            },
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/patients/{created!.Id}", update);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(updated!.MedicalHistory);
        Assert.True(updated.MedicalHistory!.HasAsthma);
        Assert.True(updated.MedicalHistory.IsPregnant);
        Assert.Equal(8, updated.MedicalHistory.PregnancyWeeks);
        Assert.Equal("122/81", updated.MedicalHistory.BloodPressureReading);
        Assert.Equal(1, updated.MedicalHistory.AlertSeverity);
    }

    [Fact]
    public async Task CreatePatient_WithInvalidBloodPressure_ShouldReturnBadRequest()
    {
        // Arrange — free text is not a blood-pressure reading.
        var request = CreatePatientRequest("Omar", "Nasir", h => h.BloodPressureReading = "high");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAlertsCount_ShouldCountPatientsWithAnyAlert()
    {
        // Arrange — three patients, two with alerts (one critical, one warning).
        await _client.PostAsJsonAsync("/api/v1.0/patients", CreatePatientRequest("Samir", "Qasim"));
        await _client.PostAsJsonAsync("/api/v1.0/patients",
            CreatePatientRequest("Rami", "Najjar", h => h.HasBleedingDisorder = true));
        await _client.PostAsJsonAsync("/api/v1.0/patients",
            CreatePatientRequest("Lina", "Fares", h => h.BloodPressureReading = "135/95"));

        // Act
        var response = await _client.GetAsync("/api/v1.0/patients/alerts-count");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Equal("2", raw);
    }

    [Fact]
    public async Task GetAlertsCount_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.ClearAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/v1.0/patients/alerts-count");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>A clean baseline patient; <paramref name="configure"/> mutates the history.</summary>
    private static CreatePatientRequest CreatePatientRequest(string firstName, string lastName)
        => CreatePatientRequest(firstName, lastName, _ => { });

    private static CreatePatientRequest CreatePatientRequest(
        string firstName,
        string lastName,
        Action<MedicalHistoryRequest> configureHistory)
    {
        var history = new MedicalHistoryRequest();
        configureHistory(history);

        return new CreatePatientRequest
        {
            FirstName = firstName,
            LastName = lastName,
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+963944555999"
            },
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = history,
        };
    }
}
