using System.Data.Common;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Tests.Common.Security;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Respawn;

using Testcontainers.MsSql;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Common;

/// <summary>
/// A SQL Server container with the real production schema but none of the
/// <c>WebAppFactory</c> seed data (that fixture pre-creates roles and an admin,
/// which would make "the seeder created them" assertions meaningless).
///
/// The fixture wires the production infrastructure (Identity, interceptors,
/// MediatR) through a plain service provider instead of a web host: the
/// bootstrap code paths are exactly the ones the CLI uses, without HTTP in the
/// way. Respawn truncates every table between tests.
/// </summary>
public sealed class SeedingFixture : IAsyncLifetime
{
    private const string DatabaseName = "BootstrapUnderTest";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    private ServiceProvider _provider = default!;
    private DbConnection _connection = default!;
    private Respawner _respawner = default!;

    /// <summary>
    /// Live host environment seen by <see cref="ApplicationDbContextInitialiser"/>.
    /// Tests flip it to prove demo seeding is Development-only.
    /// </summary>
    public MutableHostEnvironment Environment { get; } = new() { EnvironmentName = Environments.Production };

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            InitialCatalog = DatabaseName,
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["JwtSettings:Secret"] = TestConfiguration.JwtSecret,

                // Proof that the clinic profile is configuration-driven, not
                // hard-coded: these values must land in the singleton row.
                ["Bootstrap:ClinicName"] = "Fixture Dental Clinic",
                ["Bootstrap:ClinicNameArabic"] = "عيادة الفحص",
                ["Bootstrap:TimeZoneId"] = "Asia/Damascus",
                ["Bootstrap:BaseCurrency"] = "USD",
                ["Bootstrap:DefaultCountryCode"] = "963",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(Environment);
        services.AddSingleton<IUser>(new TestCurrentUser());

        // Same registrations the API composes. AddApplication supplies MediatR
        // (the domain-event interceptor depends on IPublisher);
        // AddInfrastructure supplies Identity, the DbContext and the seeder.
        services.AddApplication();
        services.AddInfrastructure(configuration);

        _provider = services.BuildServiceProvider();

        using (var scope = _provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        _connection = new SqlConnection(connectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _provider.DisposeAsync();
        await _dbContainer.StopAsync();
    }

    /// <summary>Empties every table so each test starts from a clean database.</summary>
    public Task ResetAsync() => _respawner.ResetAsync(_connection);

    public IServiceScope CreateScope() => _provider.CreateScope();

    /// <summary>Minimal <see cref="IHostEnvironment"/> so tests can drive the environment gate.</summary>
    public sealed class MutableHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "DentalClinic.Api.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}