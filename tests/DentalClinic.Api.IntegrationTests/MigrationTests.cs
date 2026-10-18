using DentalClinic.Api;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests;

/// <summary>
/// Proves the EF migration chain actually applies to a clean SQL Server database.
///
/// Every other integration test builds its schema with
/// <c>Database.EnsureCreatedAsync()</c>, which ignores migrations entirely and
/// generates the schema straight from the model. That means a migration that
/// compiles fine but fails against real SQL Server would never be caught by CI,
/// and the app (which calls <c>MigrateAsync()</c> at startup) would then fail
/// to boot in production. This test closes that gap.
/// </summary>
public class MigrationTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _webAppFactory;

    public MigrationTests(WebAppFactory webAppFactory)
    {
        _webAppFactory = webAppFactory;
    }

    [Fact]
    public async Task MigrateAsync_ShouldApplyAllMigrations_ToCleanDatabase()
    {
        using var scope = _webAppFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The fixture builds the schema with EnsureCreatedAsync, so drop it first
        // to guarantee MigrateAsync has a clean slate and must replay the whole
        // chain from InitialCreate rather than finding history already present.
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var applied = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId]")
            .ToListAsync();

        Assert.NotEmpty(applied);

        // Every known migration must be recorded, not silently skipped.
        Assert.Contains("20260912190400_InitialCreate", applied);

        var pending = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }

    [Fact]
    public async Task MigrateAsync_ShouldBeIdempotent_WhenAppliedTwice()
    {
        using var scope = _webAppFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // Re-running must be a no-op rather than an error.
        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}