using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Domain.Auditing;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

/// <summary>
/// Proves the audit trail is written automatically by the EF interceptor (not by
/// the controllers) and that the AdminOnly endpoint returns it.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class AuditLogControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public AuditLogControllerTests(WebAppFactory factory)
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
    public async Task CreatingAndUpdatingPatient_ShouldWriteCreateAndUpdateAuditEntries()
    {
        // Arrange
        var createRequest = CreateValidPatientRequest("Alice", "Smith", "+1122334455");

        // Act - create
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/patients", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created);

        // Act - update
        var updateRequest = new UpdatePatientRequest
        {
            FirstName = "AliceUpdated",
            LastName = "Smith",
            ContactInfo = new CreateContactInfoRequest { PrimaryPhone = "+1122334455" },
            DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest { HasDiabetes = true },
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1.0/patients/{created!.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Assert - the interceptor wrote the history without any controller code
        var logs = await _factory.ExecuteDbContextAsync(context =>
            context.AuditLogEntries
                .Where(entry => entry.EntityId == created.Id)
                .OrderBy(entry => entry.TimestampUtc)
                .ToListAsync());

        Assert.Contains(logs, entry => entry.Action == AuditAction.Create);
        Assert.Contains(logs, entry => entry.Action == AuditAction.Update);
        Assert.All(logs, entry => Assert.Equal("Patient", entry.EntityName));
        Assert.All(logs, entry => Assert.False(string.IsNullOrWhiteSpace(entry.UserId)));

        var create = Assert.Single(logs, entry => entry.Action == AuditAction.Create);
        Assert.Null(create.OldValues);
        Assert.Contains("Alice", create.NewValues);

        var update = Assert.Single(logs, entry => entry.Action == AuditAction.Update);
        Assert.Contains("Alice", update.OldValues);
        Assert.Contains("AliceUpdated", update.NewValues);
    }

    [Fact]
    public async Task RemovingPatient_ShouldWriteDeleteAuditEntry()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/patients",
            CreateValidPatientRequest("Mark", "Zucker", "+9988776655"));
        var created = await createResponse.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1.0/patients/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert - soft delete must still be recorded as a Delete, not an Update
        var delete = await _factory.ExecuteDbContextAsync(context =>
            context.AuditLogEntries.SingleOrDefaultAsync(entry =>
                entry.EntityId == created.Id && entry.Action == AuditAction.Delete));

        Assert.NotNull(delete);
        Assert.Equal("Patient", delete!.EntityName);
        Assert.Null(delete.NewValues);
        Assert.Contains("Mark", delete.OldValues);
    }

    [Fact]
    public async Task GetAuditLogsForEntity_ShouldReturnHistoryNewestFirst()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/patients",
            CreateValidPatientRequest("Jane", "Doe", "+1555000111"));
        var created = await createResponse.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created);

        var updateRequest = new UpdatePatientRequest
        {
            FirstName = "Janet",
            LastName = "Doe",
            ContactInfo = new CreateContactInfoRequest { PrimaryPhone = "+1555000111" },
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest(),
        };

        await _client.PutAsJsonAsync($"/api/v1.0/patients/{created!.Id}", updateRequest);

        // Act - the route name is deliberately lower-case/plural to prove it is
        // normalized onto the stored CLR name.
        var response = await _client.GetAsync($"/api/v1.0/audit-logs/patients/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content.ReadFromJsonAsync<List<AuditLogEntryDto>>();

        Assert.NotNull(history);
        Assert.True(history.Count >= 2);
        Assert.Equal("Update", history[0].Action);
        Assert.Equal("Patient", history[0].EntityName);
        Assert.Contains(history, entry => entry.Action == "Create");

        // The clinic-local projection must be populated, not left at its default.
        Assert.All(history, entry => Assert.NotEqual(default, entry.TimestampLocal));

        var timestamps = history.Select(entry => entry.TimestampUtc).ToList();
        Assert.Equal(timestamps.OrderByDescending(value => value), timestamps);
    }

    [Fact]
    public async Task GetAuditLogsForEntity_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - a fresh client with no bearer token
        var anonymous = _factory.CreateAppHttpClient();

        // Act
        var response = await anonymous.GetAsync($"/api/v1.0/audit-logs/Patient/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByEntityAndAction()
    {
        // Arrange - one patient created and updated, plus a second patient that
        // must not appear under the Update filter.
        var patient = await CreatePatientAsync("Filter", "Me", "+1555000222");
        await UpdatePatientFirstNameAsync(patient, "Filtered");
        await CreatePatientAsync("Other", "Patient", "+1555000333");

        // Act - plural/lower-case name proves the filter is normalized too.
        var response = await _client.GetAsync(
            "/api/v1.0/audit-logs?EntityName=patients&Action=Update&PageSize=50");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PaginatedList<AuditLogEntryDto>>();

        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.NotNull(page.Items);
        var entry = Assert.Single(page.Items);
        Assert.Equal("Patient", entry.EntityName);
        Assert.Equal("Update", entry.Action);
        Assert.Equal(patient, entry.EntityId);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldPageNewestFirst()
    {
        // Arrange
        var patient = await CreatePatientAsync("Page", "Turner", "+1555000444");
        await UpdatePatientFirstNameAsync(patient, "Paged");

        // Act
        var response = await _client.GetAsync("/api/v1.0/audit-logs?PageSize=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PaginatedList<AuditLogEntryDto>>();

        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
        Assert.True(page.TotalPages >= 2);
        Assert.NotNull(page.Items);
        var entry = Assert.Single(page.Items);
        Assert.Equal("Update", entry.Action);
    }

    [Fact]
    public async Task GetAuditLogs_WithDateWindowThatExcludesEverything_ShouldReturnEmptyPage()
    {
        // Arrange
        await CreatePatientAsync("Old", "News", "+1555000555");

        // A window two days ahead is in the future for every timezone on earth,
        // so it cannot contain a change written just now.
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        // Act
        var response = await _client.GetAsync(
            $"/api/v1.0/audit-logs?From={future:yyyy-MM-dd}&To={future.AddDays(1):yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PaginatedList<AuditLogEntryDto>>();

        Assert.NotNull(page);
        Assert.Equal(0, page.TotalCount);
        Assert.NotNull(page.Items);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task GetAuditLogs_WithUnknownEntityName_ShouldReturnBadRequest()
    {
        // Act - a typo must not masquerade as "no history".
        var response = await _client.GetAsync("/api/v1.0/audit-logs?EntityName=Prescriptions");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithPageSizeAboveTheCap_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/audit-logs?PageSize=500");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_AsNonAdmin_ShouldReturnForbidden()
    {
        // Arrange - a receptionist is staff, but the trail names accounts.
        // The fixture only seeds the admin account, so this test owns its actor.
        await EnsureReceptionistExistsAsync();

        var receptionist = _factory.CreateAppHttpClient();
        var token = await receptionist.GenerateTokenAsync(
            TestUsers.Receptionist.Email!,
            TestUsers.ReceptionistPassword);
        receptionist.SetAuthorizationHeader(token);

        // Act
        var response = await receptionist.GetAsync("/api/v1.0/audit-logs");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var anonymous = _factory.CreateAppHttpClient();

        // Act
        var response = await anonymous.GetAsync("/api/v1.0/audit-logs");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task EnsureReceptionistExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var receptionist = await userManager.FindByIdAsync(TestUsers.Receptionist.Id);

        if (receptionist is null)
        {
            receptionist = TestUsers.Receptionist;
            await userManager.CreateAsync(receptionist, TestUsers.ReceptionistPassword);
        }

        if (!await userManager.IsInRoleAsync(receptionist, "Receptionist"))
        {
            await userManager.AddToRoleAsync(receptionist, "Receptionist");
        }
    }

    private async Task<Guid> CreatePatientAsync(string firstName, string lastName, string phone)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1.0/patients",
            CreateValidPatientRequest(firstName, lastName, phone));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(created);

        return created!.Id;
    }

    private async Task UpdatePatientFirstNameAsync(Guid patientId, string firstName)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patientId}",
            new UpdatePatientRequest
            {
                FirstName = firstName,
                LastName = "Smith",
                ContactInfo = new CreateContactInfoRequest { PrimaryPhone = "+1555000999" },
                DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                MedicalHistory = new MedicalHistoryRequest(),
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static CreatePatientRequest CreateValidPatientRequest(string firstName, string lastName, string phone) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            ContactInfo = new CreateContactInfoRequest { PrimaryPhone = phone },
            DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            MedicalHistory = new MedicalHistoryRequest(),
        };
}
