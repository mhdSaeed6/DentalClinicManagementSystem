using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

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
///
/// It deliberately uses its own container rather than <c>WebAppFactory</c>: that
/// fixture calls <c>EnsureCreatedAsync()</c>, which would leave tables behind and
/// make a subsequent <c>MigrateAsync()</c> fail on an existing schema. Its
/// connection string also targets <c>master</c>, which cannot be dropped.
/// </summary>
public class MigrationTests : IClassFixture<MsSqlMigrationFixture>
{
    private readonly MsSqlMigrationFixture _fixture;

    public MigrationTests(MsSqlMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateContext() => new(_fixture.Options);

    [Fact]
    public async Task MigrateAsync_ShouldApplyAllMigrations_ToCleanDatabase()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        var applied = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId]")
            .ToListAsync();

        Assert.NotEmpty(applied);

        // The first migration must be present, proving the whole chain replayed
        // from zero rather than finding history already present.
        Assert.Contains("20260912190400_InitialCreate", applied);

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_ShouldBeIdempotent_WhenAppliedTwice()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        // Re-running must be a no-op rather than an error.
        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_ShouldCreateTablesWithExpectedColumns()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        // The concurrency token backing the payment race fix must actually exist
        // in the database, not merely in the EF model.
        var rowVersion = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [name] FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[Invoices]') AND [name] = 'RowVersion'")
            .ToListAsync();

        Assert.NotEmpty(rowVersion);

        // Refresh tokens must be stored as a hash column, never a raw token.
        var tokenHash = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [name] FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[RefreshTokens]') AND [name] = 'TokenHash'")
            .ToListAsync();

        Assert.NotEmpty(tokenHash);

        var rawToken = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [name] FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[RefreshTokens]') AND [name] = 'Token'")
            .ToListAsync();

        Assert.Empty(rawToken);

        // The odontogram table must exist after replaying the real migration chain.
        var patientTeethTable = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [name] FROM sys.tables WHERE [name] = 'PatientTeeth'")
            .ToListAsync();

        Assert.NotEmpty(patientTeethTable);
    }
}