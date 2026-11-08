using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.LabCases;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Contracts.Requests.Staff;
using DentalClinic.Domain.Labs;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class LabCaseControllerTests : IAsyncLifetime
{
    private const string LinkedDoctorPassword = "LabDoctor!Pass123";

    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public LabCaseControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Authorization
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetLabCases_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _client.ClearAuthorizationHeader();

        var response = await _client.GetAsync("/api/v1.0/lab-cases?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabCase_AsReceptionist_ShouldReturnForbidden()
    {
        // Arrange — receptionists may read lab cases but never mutate them.
        await EnsureClinicRolesExistAsync();

        // Doctor creation is admin-only, so build the fixtures before switching role.
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        _client.SetAuthorizationHeader(
            await _client.GenerateTokenAsync(TestUsers.Receptionist.Email!, TestUsers.ReceptionistPassword));

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1.0/lab-cases", new CreateLabCaseRequest
            {
                PatientId = patientId,
                DoctorId = doctorId,
                LabName = "Smile Lab",
                CaseType = "Crown",
                SentAtUtc = DateTimeOffset.UtcNow,
                ExpectedReturnAtUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    [Fact]
    public async Task DoctorScope_ShouldOnlyReturnAndCreateOwnLabCases()
    {
        // Arrange — two doctors, one linked Doctor account.
        await EnsureClinicRolesExistAsync();

        var ownDoctorId = await CreateTestDoctorAsync();
        var otherDoctorId = await CreateTestDoctorAsync();
        var patientId = await CreateTestPatientAsync();

        var email = $"lab.doctor.{Guid.NewGuid():N}@dentalclinic.test";

        var createAccount = await _client.PostAsJsonAsync("/api/v1.0/staff/users", new CreateStaffUserRequest
        {
            Email = email,
            Password = LinkedDoctorPassword,
            Roles = ["Doctor"]
        });

        Assert.Equal(HttpStatusCode.Created, createAccount.StatusCode);
        var account = (await createAccount.Content.ReadFromJsonAsync<DentalClinic.Application.Features.Identity.Dtos.StaffUserDto>())!;

        try
        {
            var link = await _client.PutAsJsonAsync(
                $"/api/v1.0/doctors/{ownDoctorId}/user-link",
                new LinkDoctorUserRequest { UserId = account.UserId });
            Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);

            _client.SetAuthorizationHeader(
                await _client.GenerateTokenAsync(email, LinkedDoctorPassword));

            // Act — a case for the linked doctor is accepted.
            var ownCase = await CreateLabCaseAsync(patientId, ownDoctorId);
            Assert.Equal(HttpStatusCode.Created, ownCase.StatusCode);
            var created = (await ownCase.Content.ReadFromJsonAsync<LabCaseDto>())!;
            Assert.Equal(ownDoctorId, created.DoctorId);

            // A case for another doctor is rejected by the handler-level check.
            var foreignCase = await _client.PostAsJsonAsync("/api/v1.0/lab-cases", new CreateLabCaseRequest
            {
                PatientId = patientId,
                DoctorId = otherDoctorId,
                LabName = "Smile Lab",
                CaseType = "Bridge",
                SentAtUtc = DateTimeOffset.UtcNow,
                ExpectedReturnAtUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
            Assert.Equal(HttpStatusCode.Forbidden, foreignCase.StatusCode);

            // The list is scoped to the linked doctor.
            var list = await _client.GetAsync("/api/v1.0/lab-cases?pageNumber=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);

            var page = (await list.Content.ReadFromJsonAsync<PaginatedList<LabCaseDto>>())!;
            Assert.All(page.Items!, item => Assert.Equal(ownDoctorId, item.DoctorId));
            Assert.Equal(1, page.TotalCount);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
            await DeleteAccountAsync(email);
        }
    }

    // -------------------------------------------------------------------------
    // Read + create
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetLabCases_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/v1.0/lab-cases?status=99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabCase_WithValidData_ShouldReturnCreatedCaseWithNames()
    {
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        // Act
        var response = await CreateLabCaseAsync(patientId, doctorId);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.Content.ReadFromJsonAsync<LabCaseDto>())!;
        Assert.Equal(patientId, created.PatientId);
        Assert.Equal(doctorId, created.DoctorId);
        Assert.Equal(LabCaseStatus.Sent, created.Status);
        Assert.Equal("Smile Lab", created.LabName);
        Assert.Equal("Crown", created.CaseType);
        Assert.Equal(16, created.ToothNumber);
        Assert.Equal(450m, created.Cost);
        Assert.False(created.IsOverdue);
        Assert.False(string.IsNullOrWhiteSpace(created.PatientName));
        Assert.False(string.IsNullOrWhiteSpace(created.DoctorName));

        // Read-back by id works and returns the same case.
        var byId = await _client.GetAsync($"/api/v1.0/lab-cases/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
    }

    [Fact]
    public async Task GetLabCaseById_WithUnknownId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1.0/lab-cases/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabCase_WithReturnBeforeSend_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();
        var sentAt = DateTimeOffset.UtcNow;

        var response = await _client.PostAsJsonAsync("/api/v1.0/lab-cases", new CreateLabCaseRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            LabName = "Smile Lab",
            CaseType = "Crown",
            SentAtUtc = sentAt,
            ExpectedReturnAtUtc = sentAt.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Status workflow + overdue
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ChangeLabCaseStatus_OnALegalTransition_ShouldReturnUpdatedCase()
    {
        var (created, _) = await CreateLabCaseAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.AtLab });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<LabCaseDto>())!;
        Assert.Equal(LabCaseStatus.AtLab, updated.Status);
        Assert.Null(updated.ReturnedAtUtc);
    }

    [Fact]
    public async Task ChangeLabCaseStatus_OnAnIllegalTransition_ShouldReturnConflict()
    {
        var (created, _) = await CreateLabCaseAsync();

        // Sent cannot jump straight to Returned.
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.Returned });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeLabCaseStatus_ToReturned_ShouldStampTheReturnDate()
    {
        var (created, _) = await CreateLabCaseAsync();

        await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.AtLab });

        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.Returned });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<LabCaseDto>())!;
        Assert.Equal(LabCaseStatus.Returned, updated.Status);
        Assert.NotNull(updated.ReturnedAtUtc);
    }

    [Fact]
    public async Task UpdateLabCase_OnAClosedCase_ShouldReturnConflict()
    {
        var (created, _) = await CreateLabCaseAsync();

        await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.AtLab });
        await _client.PutAsJsonAsync(
            $"/api/v1.0/lab-cases/{created.Id}/status",
            new { Status = (int)LabCaseStatus.Returned });

        var response = await _client.PutAsJsonAsync($"/api/v1.0/lab-cases/{created.Id}", new UpdateLabCaseRequest
        {
            LabName = "Another Lab",
            CaseType = "Crown",
            ExpectedReturnAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            Cost = 100m
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetLabCases_WithOverdueFilter_ShouldReturnOnlyPastDueOpenCases()
    {
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();

        var now = DateTimeOffset.UtcNow;

        // One case that should have come back five days ago and is still open.
        var overdueResponse = await CreateLabCaseAsync(
            patientId,
            doctorId,
            sentAt: now.AddDays(-10),
            expectedAt: now.AddDays(-5),
            caseType: "Overdue crown");
        Assert.Equal(HttpStatusCode.Created, overdueResponse.StatusCode);

        // One case that is still within its window.
        var currentResponse = await CreateLabCaseAsync(
            patientId,
            doctorId,
            sentAt: now,
            expectedAt: now.AddDays(5),
            caseType: "On-time bridge");
        Assert.Equal(HttpStatusCode.Created, currentResponse.StatusCode);

        var response = await _client.GetAsync("/api/v1.0/lab-cases?overdue=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = (await response.Content.ReadFromJsonAsync<PaginatedList<LabCaseDto>>())!;
        Assert.Equal(1, page.TotalCount);

        var item = Assert.Single(page.Items!);
        Assert.Equal("Overdue crown", item.CaseType);
        Assert.True(item.IsOverdue);
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteLabCase_ShouldReturnNoContent_AndHideTheCase()
    {
        var (created, _) = await CreateLabCaseAsync();

        var delete = await _client.DeleteAsync($"/api/v1.0/lab-cases/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var readBack = await _client.GetAsync($"/api/v1.0/lab-cases/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, readBack.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<(LabCaseDto Case, HttpResponseMessage Response)> CreateLabCaseAsync()
    {
        var patientId = await CreateTestPatientAsync();
        var doctorId = await CreateTestDoctorAsync();
        var response = await CreateLabCaseAsync(patientId, doctorId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return ((await response.Content.ReadFromJsonAsync<LabCaseDto>())!, response);
    }

    private Task<HttpResponseMessage> CreateLabCaseAsync(
        Guid patientId,
        Guid doctorId,
        DateTimeOffset? sentAt = null,
        DateTimeOffset? expectedAt = null,
        string caseType = "Crown") =>
        _client.PostAsJsonAsync("/api/v1.0/lab-cases", new CreateLabCaseRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ToothNumber = 16,
            LabName = "Smile Lab",
            CaseType = caseType,
            SentAtUtc = sentAt ?? DateTimeOffset.UtcNow,
            ExpectedReturnAtUtc = expectedAt ?? DateTimeOffset.UtcNow.AddDays(7),
            Cost = 450m,
            Notes = "shade A2"
        });

    private async Task<Guid> CreateTestPatientAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", new CreatePatientRequest
        {
            FirstName = "Lab",
            LastName = "Patient",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1234567890"
            },
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasDiabetes = false,
                HasHeartDisease = false
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var patient = (await response.Content.ReadFromJsonAsync<PatientDto>())!;
        return patient.Id;
    }

    private async Task<Guid> CreateTestDoctorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", new CreateDoctorRequest
        {
            FirstName = "Lab",
            LastName = $"Doctor{Guid.NewGuid():N}"[..12],
            Specialization = "Prosthodontics",
            Gender = "Male",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+1234567890"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var doctor = (await response.Content.ReadFromJsonAsync<DoctorDto>())!;
        return doctor.Id;
    }

    private async Task EnsureClinicRolesExistAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var roleName in new[] { "Admin", "Doctor", "Receptionist" })
        {
            if (await roleManager.FindByNameAsync(roleName) is null)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

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

    private async Task DeleteAccountAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }
    }
}