using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Contracts.Requests.TreatmentRecords;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors; // أو المسار المخصص لكيان الـ Doctor
using DentalClinic.Tests.Common.Security;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class TreatmentRecordControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public TreatmentRecordControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        // المصادقة لتجاوز [Authorize]
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/treatment-records
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTreatmentRecords_WithDefaultPagination_ShouldReturnPaginatedList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/treatment-records?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<TreatmentRecordDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    // -------------------------------------------------------------------------
    // 2. GET /api/v1.0/treatment-records/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTreatmentRecordById_WithValidId_ShouldReturnRecord()
    {
        // Arrange
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync(); // إنشاء دكتور حقيقي في الـ DbContext

        var createRequest = new CreateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 11,
            ProcedureDetails = "Initial cleaning and checkup",
            Cost = 100.00m
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdRecord = await createResponse.Content.ReadFromJsonAsync<TreatmentRecordDto>();
        Assert.NotNull(createdRecord);

        // Act
        var response = await _client.GetAsync($"/api/v1.0/treatment-records/{createdRecord.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TreatmentRecordDto>();
        Assert.NotNull(result);
        Assert.Equal(createdRecord.Id, result!.Id);
        Assert.Equal(patientId, result.PatientId);
        Assert.Equal(doctorId, result.DoctorId);
    }

    [Fact]
    public async Task GetTreatmentRecordById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/treatment-records/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. POST /api/v1.0/treatment-records
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTreatmentRecord_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        var request = new CreateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 21,
            ProcedureDetails = "Composite filling on tooth 21",
            Cost = 150.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdRecord = await response.Content.ReadFromJsonAsync<TreatmentRecordDto>();
        Assert.NotNull(createdRecord);
        Assert.Equal(patientId, createdRecord.PatientId);
        Assert.Equal(doctorId, createdRecord.DoctorId);
        Assert.Equal(21, createdRecord.ToothNumber);
        Assert.Equal(150.00m, createdRecord.Cost);

        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateTreatmentRecord_WithInvalidToothNumber_ShouldReturnBadRequest()
    {
        // Arrange
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        var request = new CreateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 99, // رقم سن غير صالح FDI
            ProcedureDetails = "Invalid tooth test",
            Cost = 50.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 4. PUT /api/v1.0/treatment-records/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateTreatmentRecord_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        var createRequest = new CreateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 16,
            ProcedureDetails = "Tooth extraction preparation",
            Cost = 100.00m
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", createRequest);
        var createdRecord = await createResponse.Content.ReadFromJsonAsync<TreatmentRecordDto>();

        var updateRequest = new UpdateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 16,
            ProcedureDetails = "Surgical extraction completed",
            Cost = 250.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/treatment-records/{createdRecord!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedRecord = await response.Content.ReadFromJsonAsync<TreatmentRecordDto>();
        Assert.NotNull(updatedRecord);
        Assert.Equal("Surgical extraction completed", updatedRecord.ProcedureDetails);
        Assert.Equal(250.00m, updatedRecord.Cost);
    }

    [Fact]
    public async Task UpdateTreatmentRecord_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        var updateRequest = new UpdateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = null,
            ToothNumber = 11,
            ProcedureDetails = "Non existing record test",
            Cost = 100.00m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/treatment-records/{nonExistingId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private async Task<Guid> CreateTestPatientAsync()
    {
        var patientRequest = new CreatePatientRequest
        {
            FirstName = "Test",
            LastName = "Patient",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1234567890",
            },
            DateOfBirth = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasDiabetes = false,
                HasHeartDisease = false,
                Allergies = null,
                Notes = null
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", patientRequest);
        var patient = await response.Content.ReadFromJsonAsync<PatientDto>();
        return patient!.Id;
    }

    private async Task<Guid> CreateTestDoctorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var contactInfo = ContactInfo.Create("+1234567890", null, true, null).Value;

        var doctorResult = Doctor.Create(
            "John",
            "Doe",
            "General Dentistry",
            contactInfo,
            Gender.Male);

        var doctor = doctorResult.Value;

        dbContext.Doctors.Add(doctor);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return doctor.Id;
    }
}