using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Staff;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class StaffUserControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();
    private readonly WebAppFactory _factory = webAppFactory;

    private const string StaffTestPassword = "Staff!Pass1234";

    private const string CreatedEmail = "created.staff@dentalclinic.test";
    private const string UnknownRoleEmail = "unknown.role@dentalclinic.test";
    private const string ToggleEmail = "toggle.staff@dentalclinic.test";
    private const string ResetEmail = "reset.staff@dentalclinic.test";
    private const string LinkEmail = "link.doctor@dentalclinic.test";

    // -------------------------------------------------------------------------
    // Authorization policy
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetStaffUsers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/staff/users?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Doctor")]
    [InlineData("Receptionist")]
    public async Task GetStaffUsers_WithNonAdminRole_ShouldReturnForbidden(string roleKey)
    {
        // Arrange — doctor / receptionist accounts are created per test here
        // because the factory seeds only the admin.
        await EnsureClinicRolesExistAsync();

        var (user, password) = roleKey == "Receptionist"
            ? (TestUsers.Receptionist, TestUsers.ReceptionistPassword)
            : (TestUsers.Doctor, TestUsers.DoctorPassword);

        var token = await _client.GenerateTokenAsync(user.Email!, password);
        _client.SetAuthorizationHeader(token);

        try
        {
            // Act
            var response = await _client.GetAsync("/api/v1.0/staff/users?pageNumber=1&pageSize=10");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    // -------------------------------------------------------------------------
    // Create + roles
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateStaffUser_WithValidData_ShouldCreateAccountAbleToLogIn()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateStaffUserRequest
        {
            Email = CreatedEmail,
            Password = StaffTestPassword,
            Roles = ["Receptionist"]
        };

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1.0/staff/users", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<StaffUserDto>();
            Assert.NotNull(created);
            Assert.Equal(request.Email, created!.Email);
            Assert.Contains("Receptionist", created.Roles);

            // The account must be able to authenticate immediately.
            _client.ClearAuthorizationHeader();
            var login = await LoginAsync(request.Email, StaffTestPassword);
            Assert.True(login.IsSuccessStatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    [Fact]
    public async Task CreateStaffUser_WithUnknownRole_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateStaffUserRequest
        {
            Email = UnknownRoleEmail,
            Password = StaffTestPassword,
            Roles = ["Owner"]
        };

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1.0/staff/users", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    [Fact]
    public async Task UpdateStaffUserRoles_OnOwnAccount_ShouldReturnConflict()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        try
        {
            // Act
            var response = await _client.PutAsJsonAsync(
                $"/api/v1.0/staff/users/{TestUsers.Admin.Id}/roles",
                new UpdateStaffUserRolesRequest { Roles = ["Receptionist"] });

            // Assert — an admin must not be able to demote or strand themselves.
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    // -------------------------------------------------------------------------
    // Deactivation ("lockout") + password reset
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetStaffUserActive_DeactivateThenReactivate_ShouldToggleLogin()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var create = await _client.PostAsJsonAsync("/api/v1.0/staff/users", new CreateStaffUserRequest
        {
            Email = ToggleEmail,
            Password = StaffTestPassword,
            Roles = ["Receptionist"]
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = (await create.Content.ReadFromJsonAsync<StaffUserDto>())!;

        try
        {
            // Act — deactivate
            var deactivate = await _client.PutAsJsonAsync(
                $"/api/v1.0/staff/users/{created.UserId}/status",
                new SetStaffUserActiveRequest { IsActive = false });

            // Assert — login now fails with the uniform error
            Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

            _client.ClearAuthorizationHeader();
            var failedLogin = await LoginAsync(ToggleEmail, StaffTestPassword);
            Assert.False(failedLogin.IsSuccessStatusCode);

            // Act — reactivate
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
            var reactivate = await _client.PutAsJsonAsync(
                $"/api/v1.0/staff/users/{created.UserId}/status",
                new SetStaffUserActiveRequest { IsActive = true });

            // Assert — login works again
            Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
            _client.ClearAuthorizationHeader();
            var successfulLogin = await LoginAsync(ToggleEmail, StaffTestPassword);
            Assert.True(successfulLogin.IsSuccessStatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
            await DeleteAccountAsync(ToggleEmail);
        }
    }

    [Fact]
    public async Task ResetStaffUserPassword_ShouldSwapTheWorkingPassword()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        const string newPassword = "Staff!Password99";

        var create = await _client.PostAsJsonAsync("/api/v1.0/staff/users", new CreateStaffUserRequest
        {
            Email = ResetEmail,
            Password = StaffTestPassword,
            Roles = ["Receptionist"]
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = (await create.Content.ReadFromJsonAsync<StaffUserDto>())!;

        try
        {
            // Act
            var reset = await _client.PutAsJsonAsync(
                $"/api/v1.0/staff/users/{created.UserId}/password",
                new ResetStaffUserPasswordRequest { NewPassword = newPassword });

            // Assert
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            _client.ClearAuthorizationHeader();
            var oldLogin = await LoginAsync(ResetEmail, StaffTestPassword);
            Assert.False(oldLogin.IsSuccessStatusCode);

            var newLogin = await LoginAsync(ResetEmail, newPassword);
            Assert.True(newLogin.IsSuccessStatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
            await DeleteAccountAsync(ResetEmail);
        }
    }

    // -------------------------------------------------------------------------
    // Doctor <-> user link
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LinkDoctorUser_ShouldReturnNoContent_AndConflictWhenRelinked()
    {
        // Arrange
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var doctorResponse = await _client.PostAsJsonAsync("/api/v1.0/doctors", new
        {
            FirstName = "Link",
            LastName = "Tester",
            Specialization = "Orthodontics",
            Gender = "Male",
            ContactInfo = new { PrimaryPhone = "0998765432", HasWhatsAppOnPrimary = false }
        });

        Assert.Equal(HttpStatusCode.Created, doctorResponse.StatusCode);
        var doctor = (await doctorResponse.Content.ReadFromJsonAsync<DentalClinic.Application.Features.Doctors.Dtos.DoctorDto>())!;

        var staffResponse = await _client.PostAsJsonAsync("/api/v1.0/staff/users", new CreateStaffUserRequest
        {
            Email = LinkEmail,
            Password = StaffTestPassword,
            Roles = ["Doctor"]
        });

        Assert.Equal(HttpStatusCode.Created, staffResponse.StatusCode);
        var account = (await staffResponse.Content.ReadFromJsonAsync<StaffUserDto>())!;

        var secondDoctorResponse = await _client.PostAsJsonAsync("/api/v1.0/doctors", new
        {
            FirstName = "Conflict",
            LastName = "Tester",
            Specialization = "Prosthodontics",
            Gender = "Female",
            ContactInfo = new { PrimaryPhone = "0998765431", HasWhatsAppOnPrimary = false }
        });

        Assert.Equal(HttpStatusCode.Created, secondDoctorResponse.StatusCode);
        var secondDoctor = (await secondDoctorResponse.Content.ReadFromJsonAsync<DentalClinic.Application.Features.Doctors.Dtos.DoctorDto>())!;

        try
        {
            // Act — link
            var link = await _client.PutAsJsonAsync(
                $"/api/v1.0/doctors/{doctor.Id}/user-link",
                new LinkDoctorUserRequest { UserId = account.UserId });

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);

            // A second doctor cannot claim the same account.
            var conflict = await _client.PutAsJsonAsync(
                $"/api/v1.0/doctors/{secondDoctor.Id}/user-link",
                new LinkDoctorUserRequest { UserId = account.UserId });

            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

            // Unlink works, and the freed account can be linked again.
            var unlink = await _client.DeleteAsync($"/api/v1.0/doctors/{doctor.Id}/user-link");
            Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);

            var relink = await _client.PutAsJsonAsync(
                $"/api/v1.0/doctors/{secondDoctor.Id}/user-link",
                new LinkDoctorUserRequest { UserId = account.UserId });
            Assert.Equal(HttpStatusCode.NoContent, relink.StatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
            await _client.DeleteAsync($"/api/v1.0/doctors/{doctor.Id}");
            await _client.DeleteAsync($"/api/v1.0/doctors/{secondDoctor.Id}");
            await DeleteAccountAsync(LinkEmail);
        }
    }

    [Fact]
    public async Task LinkDoctorUser_NonAdmin_ShouldReturnForbidden()
    {
        // Arrange
        await EnsureClinicRolesExistAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var doctors = await _client.GetAsync("/api/v1.0/doctors?pageNumber=1&pageSize=1");
        doctors.EnsureSuccessStatusCode();
        var list = (await doctors.Content.ReadFromJsonAsync<PaginatedList<DentalClinic.Application.Features.Doctors.Dtos.DoctorDto>>())!;

        Guid doctorId = list.Items!.Count > 0 ? list.Items!.First().Id : Guid.NewGuid();

        var receptionistToken = await _client.GenerateTokenAsync(
            TestUsers.Receptionist.Email!,
            TestUsers.ReceptionistPassword);
        _client.SetAuthorizationHeader(receptionistToken);

        try
        {
            // Act
            var link = await _client.PutAsJsonAsync(
                $"/api/v1.0/doctors/{doctorId}/user-link",
                new LinkDoctorUserRequest { UserId = TestUsers.Receptionist.Id });

            // Assert — 403 from the AdminOnly policy, or 403 from the
            // handler-level role check; either is a rejection.
            Assert.Equal(HttpStatusCode.Forbidden, link.StatusCode);
        }
        finally
        {
            _client.ClearAuthorizationHeader();
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        await _client.PostAsJsonAsync(
            "api/v1/identity/token/generate",
            new { Email = email, Password = password });

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

        // TestUsers.* are per-access factories (a fresh AppUser each time), so one
        // local instance must be reused across Create/AddToRole inside a scope —
        // otherwise Identity attaches a second tracked instance and throws. The
        // role check also heals accounts left without a role by earlier failed runs.
        var doctorUser = await userManager.FindByIdAsync(TestUsers.Doctor.Id);

        if (doctorUser is null)
        {
            doctorUser = TestUsers.Doctor;
            await userManager.CreateAsync(doctorUser, TestUsers.DoctorPassword);
        }

        if (!await userManager.IsInRoleAsync(doctorUser, "Doctor"))
        {
            await userManager.AddToRoleAsync(doctorUser, "Doctor");
        }

        var receptionistUser = await userManager.FindByIdAsync(TestUsers.Receptionist.Id);

        if (receptionistUser is null)
        {
            receptionistUser = TestUsers.Receptionist;
            await userManager.CreateAsync(receptionistUser, TestUsers.ReceptionistPassword);
        }

        if (!await userManager.IsInRoleAsync(receptionistUser, "Receptionist"))
        {
            await userManager.AddToRoleAsync(receptionistUser, "Receptionist");
        }
    }

    /// <summary>
    /// Removes a staff-test account. Respawn ignores AspNetUsers, so test
    /// accounts are cleaned up explicitly to keep runs deterministic.
    /// </summary>
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