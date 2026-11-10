using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Covers the production bootstrap contract: system seeding creates only what
/// every environment needs (roles + the clinic profile), demo seeding never runs
/// outside Development, and the first administrator can be created idempotently
/// from configuration.
/// </summary>
public class BootstrapSeedingTests : IClassFixture<SeedingFixture>, IAsyncLifetime
{
    private const string BootstrapEmail = "owner@bootstrap.example";
    private const string BootstrapPassword = "Owner!Bootstrap123";

    private readonly SeedingFixture _fixture;

    public BootstrapSeedingTests(SeedingFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fixture.Environment.EnvironmentName = Environments.Production;

        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedSystemDataAsync_CreatesRolesAndClinicProfile_WithoutAnyDemoData()
    {
        await RunAsync(initialiser => initialiser.SeedSystemDataAsync());

        using var scope = _fixture.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var role in ApplicationDbContextInitialiser.SystemRoles)
        {
            Assert.True(await roleManager.RoleExistsAsync(role), $"The '{role}' role was not seeded.");
        }

        // The singleton profile comes from the Bootstrap:* configuration, proving
        // the seeder is not hard-coded to the demo values.
        var clinic = await context.ClinicSettings.SingleAsync();
        Assert.Equal("Fixture Dental Clinic", clinic.Name);
        Assert.Equal("عيادة الفحص", clinic.NameArabic);
        Assert.Equal("Asia/Damascus", clinic.TimeZoneId);
        Assert.Equal("USD", clinic.BaseCurrency);
        Assert.Equal("963", clinic.DefaultCountryCode);

        // No demo dataset may leak into a system seed.
        Assert.Empty(await context.Patients.ToListAsync());
        Assert.Empty(await context.Doctors.ToListAsync());
        Assert.Empty(await context.DentalServices.ToListAsync());
        Assert.Empty(await context.Appointments.ToListAsync());
        Assert.Empty(await context.DentalServices.ToListAsync());
        Assert.Empty(await context.PatientTeeth.ToListAsync());

        // ... including the known-password demo accounts.
        Assert.Null(await userManager.FindByEmailAsync("admin@dentalclinic.com"));
        Assert.Null(await userManager.FindByEmailAsync("dr.smith@dentalclinic.com"));
        Assert.Null(await userManager.FindByEmailAsync("dr.sara@dentalclinic.com"));
        Assert.Null(await userManager.FindByEmailAsync("reception@dentalclinic.com"));
    }

    [Fact]
    public async Task SeedSystemDataAsync_IsIdempotent_WhenRunRepeatedly()
    {
        await RunAsync(initialiser => initialiser.SeedSystemDataAsync());
        await RunAsync(initialiser => initialiser.SeedSystemDataAsync());

        using var scope = _fixture.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await context.ClinicSettings.CountAsync());

        foreach (var role in ApplicationDbContextInitialiser.SystemRoles)
        {
            Assert.True(await roleManager.RoleExistsAsync(role));
        }
    }

    [Fact]
    public async Task BootstrapAdminAsync_CreatesAdminAccount_AndAssignsAdminRole()
    {
        await RunAsync(initialiser => initialiser.BootstrapAdminAsync(
            new BootstrapOptions
            {
                AdminEmail = BootstrapEmail,
                AdminPassword = BootstrapPassword,
            }));

        using var scope = _fixture.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await userManager.FindByEmailAsync(BootstrapEmail);

        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
        Assert.True(await userManager.CheckPasswordAsync(user, BootstrapPassword));
        Assert.True(await userManager.IsInRoleAsync(user, "Admin"));

        // The profile is seeded by the same command, so a fresh database is
        // immediately usable after --bootstrap-admin.
        Assert.Equal(1, await context.ClinicSettings.CountAsync());
        Assert.Empty(await context.Patients.ToListAsync());
    }

    [Fact]
    public async Task BootstrapAdminAsync_IsIdempotent_WhenRunTwiceWithTheSameCredentials()
    {
        var options = new BootstrapOptions
        {
            AdminEmail = BootstrapEmail,
            AdminPassword = BootstrapPassword,
        };

        await RunAsync(initialiser => initialiser.BootstrapAdminAsync(options));
        await RunAsync(initialiser => initialiser.BootstrapAdminAsync(options));

        using var scope = _fixture.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await context.Users.CountAsync(u => u.Email == BootstrapEmail));

        var user = await userManager.FindByEmailAsync(BootstrapEmail);

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, "Admin"));
        Assert.True(await userManager.CheckPasswordAsync(user!, BootstrapPassword));
    }

    [Fact]
    public async Task BootstrapAdminAsync_DoesNotResetAnExistingAdminPassword()
    {
        await RunAsync(initialiser => initialiser.BootstrapAdminAsync(
            new BootstrapOptions
            {
                AdminEmail = BootstrapEmail,
                AdminPassword = BootstrapPassword,
            }));

        // A second run with a different password must not silently rotate the
        // credential: the account may have been taken over deliberately.
        await RunAsync(initialiser => initialiser.BootstrapAdminAsync(
            new BootstrapOptions
            {
                AdminEmail = BootstrapEmail,
                AdminPassword = "SomeoneElse!12345",
            }));

        using var scope = _fixture.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(BootstrapEmail);

        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(user!, BootstrapPassword));
        Assert.True(await userManager.IsInRoleAsync(user!, "Admin"));
    }

    [Fact]
    public async Task BootstrapAdminAsync_Throws_WhenEmailIsMissing()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunAsync(initialiser => initialiser.BootstrapAdminAsync(
                new BootstrapOptions
                {
                    AdminPassword = BootstrapPassword,
                })));

        Assert.Contains("Bootstrap:AdminEmail", exception.Message, StringComparison.Ordinal);

        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Validation happens before the database is touched.
        Assert.Equal(0, await context.Users.CountAsync());
        Assert.Empty(await context.ClinicSettings.ToListAsync());
    }

    [Fact]
    public async Task BootstrapAdminAsync_Throws_WhenPasswordIsMissing()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunAsync(initialiser => initialiser.BootstrapAdminAsync(
                new BootstrapOptions
                {
                    AdminEmail = BootstrapEmail,
                })));

        Assert.Contains("Bootstrap:AdminPassword", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BootstrapAdminAsync_Throws_AndCreatesNoAccount_WhenPasswordViolatesThePolicy()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunAsync(initialiser => initialiser.BootstrapAdminAsync(
                new BootstrapOptions
                {
                    AdminEmail = BootstrapEmail,
                    AdminPassword = "weak",
                })));

        Assert.Contains("Could not create the bootstrap administrator", exception.Message, StringComparison.Ordinal);

        using var scope = _fixture.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        Assert.Null(await userManager.FindByEmailAsync(BootstrapEmail));
    }

    [Fact]
    public async Task SeedDemoDataAsync_SeedsTheDemoDataset_InDevelopment()
    {
        _fixture.Environment.EnvironmentName = Environments.Development;

        // The startup path seeds system data (roles + clinic profile) before the
        // demo dataset; the demo accounts need those roles to exist.
        await RunAsync(initialiser => initialiser.SeedSystemDataAsync());
        await RunAsync(initialiser => initialiser.SeedDemoDataAsync());

        using var scope = _fixture.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.NotEmpty(await context.Patients.ToListAsync());
        Assert.NotEmpty(await context.Doctors.ToListAsync());
        Assert.NotEmpty(await context.DentalServices.ToListAsync());
        Assert.NotNull(await userManager.FindByEmailAsync("admin@dentalclinic.com"));
    }

    [Fact]
    public async Task SeedDemoDataAsync_IsIgnored_OutsideDevelopment()
    {
        // InitializeAsync already set Production; this test spells it out so the
        // guard cannot be removed silently.
        _fixture.Environment.EnvironmentName = Environments.Production;

        await RunAsync(initialiser => initialiser.SeedDemoDataAsync());

        using var scope = _fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await context.Patients.ToListAsync());
        Assert.Empty(await context.Doctors.ToListAsync());
        Assert.Empty(await context.DentalServices.ToListAsync());
    }

    /// <summary>
    /// Runs an initialiser action in its own scope, like a CLI invocation or a
    /// scoped request would.
    /// </summary>
    private async Task RunAsync(Func<ApplicationDbContextInitialiser, Task> action)
    {
        using var scope = _fixture.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await action(initialiser);
    }
}