using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Odontograms;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

/// <summary>
/// End-to-end odontogram tests: they authenticate with real JWTs issued by the
/// identity endpoint and exercise the two-layer authorization actually wired on
/// the controller (StaffOnly / ClinicalWrite policies plus the server-derived
/// patient scope consumed by the handlers).
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class OdontogramControllerTests : IAsyncLifetime
{
    /// <summary>
    /// Password of the throwaway, role-less account. It only has to satisfy
    /// the Identity password policy - the account is never a doctor.
    /// </summary>
    private const string NonStaffPassword = "NonStaff!Pass123";

    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public OdontogramControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        // Real JWT; the default caller is the admin, scope-denial tests swap tokens.
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/patients/{patientId}/odontogram
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOdontogram_ForFreshPatient_ShouldReturnEmptyTeethAndHistory()
    {
        // Arrange: a patient starts "unknown until touched" — no condition rows,
        // no history (plan decision 2: no backfill).
        var patient = await SeedPatientAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/patients/{patient.Id}/odontogram");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PatientOdontogramDto>();
        Assert.NotNull(result);
        Assert.Equal(patient.Id, result!.PatientId);
        Assert.Empty(result.Teeth);
        Assert.Empty(result.History);
    }

    [Fact]
    public async Task GetOdontogram_ForUnknownPatient_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1.0/patients/{Guid.NewGuid()}/odontogram");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. PUT /api/v1.0/patients/{patientId}/odontogram/teeth/{toothNumber}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpsertToothCondition_ForToothWithoutRow_ShouldCreateItAndGetReflectsIt()
    {
        // Arrange: the upsert create leg — a fresh patient has no row for tooth 26.
        var patient = await SeedPatientAsync();
        var request = new UpsertToothConditionRequest
        {
            Condition = 1, // Caries
            Surface = 0,   // Occlusal
            Notes = "Occlusal caries observed."
        };

        // Act
        var putResponse = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            request);

        // Assert: the response carries the stored summary.
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var created = await putResponse.Content.ReadFromJsonAsync<PatientToothSummaryDto>();
        Assert.NotNull(created);
        Assert.Equal(26, created!.ToothNumber);
        Assert.Equal(ToothCondition.Caries, created.Condition);
        Assert.Equal(ToothSurface.Occlusal, created.Surface);
        Assert.Equal("Occlusal caries observed.", created.Notes);
        Assert.NotNull(created.LastModifiedUtc);

        // And a following GET reflects the stored state.
        var odontogram = await GetOdontogramAsync(patient.Id);
        var summary = Assert.Single(odontogram!.Teeth);
        Assert.Equal(26, summary.ToothNumber);
        Assert.Equal(ToothCondition.Caries, summary.Condition);
        Assert.Equal(ToothSurface.Occlusal, summary.Surface);
        Assert.Equal("Occlusal caries observed.", summary.Notes);
    }

    [Fact]
    public async Task UpsertToothCondition_ForToothWithRow_ShouldUpdateTheSameRow()
    {
        // Arrange: tooth 26 already carries a condition; the second PUT is the
        // update leg and must not produce a second row for the same tooth.
        var patient = await SeedPatientAsync();
        await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest
            {
                Condition = 1, // Caries
                Surface = 0,   // Occlusal
                Notes = "Occlusal caries observed."
            });

        // Act
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest
            {
                Condition = 2, // Filled
                Surface = 2,   // Distal
                Notes = "Restored with composite."
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<PatientToothSummaryDto>();
        Assert.NotNull(updated);
        Assert.Equal(ToothCondition.Filled, updated!.Condition);
        Assert.Equal(ToothSurface.Distal, updated.Surface);
        Assert.Equal("Restored with composite.", updated.Notes);

        var odontogram = await GetOdontogramAsync(patient.Id);
        var summary = Assert.Single(odontogram!.Teeth);
        Assert.Equal(26, summary.ToothNumber);
        Assert.Equal(ToothCondition.Filled, summary.Condition);
    }

    [Fact]
    public async Task UpsertToothCondition_WithUndefinedEnumValues_ShouldReturnBadRequest()
    {
        // Arrange: ints outside the annotated ranges are undefined enum values;
        // they must be rejected as client errors, never surface as a 500.
        var patient = await SeedPatientAsync();

        // Act + Assert: an undefined condition value.
        var undefinedCondition = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 8 });

        Assert.Equal(HttpStatusCode.BadRequest, undefinedCondition.StatusCode);

        // And an undefined surface value (the condition itself is valid here).
        var undefinedSurface = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 1, Surface = 6 });

        Assert.Equal(HttpStatusCode.BadRequest, undefinedSurface.StatusCode);
    }

    [Fact]
    public async Task UpsertToothCondition_WithInvalidToothNumber_ShouldReturnBadRequest()
    {
        // Arrange: tooth 19 is not a valid FDI number (quadrant 1 ends at 18);
        // the single FDI source of truth rejects it in the command validator.
        var patient = await SeedPatientAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/19",
            new UpsertToothConditionRequest { Condition = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpsertToothCondition_ForUnknownPatient_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{Guid.NewGuid()}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpsertToothCondition_AsReceptionist_ShouldReturnForbidden()
    {
        // Arrange: receptionists are staff but hold no clinical write capability;
        // the ClinicalWrite policy denies the write before any handler code runs.
        var patient = await SeedPatientAsync();
        var receptionistEmail = await EnsureReceptionistAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpsertToothCondition_ConcurrentUpsertsOnFreshTooth_ShouldNeverReturnServerError()
    {
        // Arrange: concurrent first-inserts on the same tooth race the filtered
        // unique index; the loser must surface as 409 (Tooth.ConditionConflict),
        // never as a 500. The race itself is not deterministic — when the requests
        // serialize instead, every response is 200 — so the assertion accepts both
        // outcomes and rejects anything else.
        var patient = await SeedPatientAsync();
        var request = new UpsertToothConditionRequest
        {
            Condition = 1, // Caries
            Surface = 0,   // Occlusal
            Notes = "Concurrent upsert."
        };

        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => _client.PutAsJsonAsync(
                $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
                request)));

        // Assert
        Assert.All(responses, response => Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Unexpected status code {response.StatusCode}."));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);

        // The filtered unique index guarantees a single live row whatever the
        // race outcome was.
        var liveRowCount = await _factory.ExecuteDbContextAsync(async context =>
            await context.PatientTeeth
                .CountAsync(t => t.PatientId == patient.Id && t.ToothNumber == 26));

        Assert.Equal(1, liveRowCount);
    }

    // -------------------------------------------------------------------------
    // 3. DELETE /api/v1.0/patients/{patientId}/odontogram/teeth/{toothNumber}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveToothCondition_Twice_ShouldClearAndStayIdempotent()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 3 }); // Crown

        // Act: the first delete clears the live row.
        var firstDelete = await _client.DeleteAsync($"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstDelete.StatusCode);

        var odontogramAfterClear = await GetOdontogramAsync(patient.Id);
        Assert.Empty(odontogramAfterClear!.Teeth);

        // The second delete is an idempotent success (no live row to remove).
        var secondDelete = await _client.DeleteAsync($"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26");

        Assert.Equal(HttpStatusCode.OK, secondDelete.StatusCode);

        var odontogramAfterSecondClear = await GetOdontogramAsync(patient.Id);
        Assert.Empty(odontogramAfterSecondClear!.Teeth);
    }

    // -------------------------------------------------------------------------
    // 4. Patient-registry scope and the staff-only policy
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Odontogram_AsAuthenticatedNoRoleUser_ShouldReturnForbiddenOnReadAndWrite()
    {
        // Arrange: an authenticated identity that holds no clinic role is the one
        // identity class the trusted patient scope restricts (fail-closed); the
        // staff-only policy denies it on this surface before any handler code runs.
        var patient = await SeedPatientAsync();
        var email = await EnsureNoStaffRoleUserAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(email, NonStaffPassword));

        // Act + Assert: read.
        var getResponse = await _client.GetAsync($"/api/v1.0/patients/{patient.Id}/odontogram");

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);

        // And write.
        var putResponse = await _client.PutAsJsonAsync(
            $"/api/v1.0/patients/{patient.Id}/odontogram/teeth/26",
            new UpsertToothConditionRequest { Condition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
    }

    [Fact]
    public async Task GetOdontogram_AsDoctor_ForUnrelatedPatient_ShouldReturnOk()
    {
        // Arrange: all staff share the full patient registry (AUDIT §8-10), so a
        // doctor may read the odontogram of a patient they have no link to; a
        // per-doctor odontogram restriction is a future decision, not this phase.
        var (doctorEmail, _) = await CreateDoctorUserAsync("odontogram.read.doc@dentalclinic.com");
        var patient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword));

        // Act
        var response = await _client.GetAsync($"/api/v1.0/patients/{patient.Id}/odontogram");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PatientOdontogramDto>();
        Assert.NotNull(result);
        Assert.Equal(patient.Id, result!.PatientId);
        Assert.Empty(result.Teeth);
        Assert.Empty(result.History);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private async Task<PatientOdontogramDto?> GetOdontogramAsync(Guid patientId)
    {
        var response = await _client.GetAsync($"/api/v1.0/patients/{patientId}/odontogram");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<PatientOdontogramDto>();
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var patient = PatientTestDataBuilder.Create().Build();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync(default);
        });

        return patient;
    }

    /// <summary>
    /// Creates (or reuses) a doctor account with a linked Doctor profile, so the
    /// issued JWT is a real doctor identity. Identity rows survive Respawn, hence
    /// the unique email per test; the Doctor profile is recreated because Respawn
    /// wipes it.
    /// </summary>
    private async Task<(string Email, Doctor Doctor)> CreateDoctorUserAsync(string email)
    {
        AppUser user;

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            user = (await userManager.FindByEmailAsync(email))!;

            if (user is null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, TestUsers.DoctorPassword);
                Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, "Doctor"))
            {
                await userManager.AddToRoleAsync(user, "Doctor");
            }
        }

        Doctor doctor = null!;
        await _factory.ExecuteDbContextAsync(async context =>
        {
            doctor = DoctorTestDataBuilder.Create().WithName("Odontogram", "Doctor").Build();
            doctor.AssignUser(user.Id);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (email, doctor);
    }

    private async Task<string> EnsureReceptionistAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var email = TestUsers.Receptionist.Email!;
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = TestUsers.Receptionist;
            var created = await userManager.CreateAsync(user, TestUsers.ReceptionistPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, "Receptionist"))
        {
            await userManager.AddToRoleAsync(user, "Receptionist");
        }

        return email;
    }

    /// <summary>
    /// An authenticated identity that holds no clinic role at all. It proves the
    /// staff-only policy is really enforced and not merely "any token".
    /// </summary>
    private async Task<string> EnsureNoStaffRoleUserAsync()
    {
        const string email = "odontogram.nonstaff@dentalclinic.com";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(user, NonStaffPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        // No role is assigned on purpose: the account must hold none of the
        // clinic roles (Admin, Doctor, Receptionist).
        return email;
    }
}