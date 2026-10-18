using DentalClinic.Infrastructure.Data;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Common;

/// <summary>
/// Owns a single SQL Server container for the migration tests.
///
/// Registered as a class fixture rather than relying on <c>IAsyncLifetime</c> on
/// the test class: xUnit creates a new test-class instance (and therefore runs
/// <c>InitializeAsync</c>) for every test method, which would start one container
/// per test. Sharing one container keeps this to a single ~10s spin-up.
/// </summary>
public sealed class MsSqlMigrationFixture : IAsyncLifetime
{
    public DbContextOptions<AppDbContext> Options { get; private set; } = default!;

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Point at a real (empty) database rather than master so the migration
        // chain creates the schema from scratch. Testcontainers' connection
        // string has no Initial Catalog, and the named GetConnectionString
        // overload is not supported by the MsSql provider.
        var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            InitialCatalog = "MigrationsUnderTest",
        }.ConnectionString;

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}