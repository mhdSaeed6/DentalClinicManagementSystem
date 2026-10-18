using DentalClinic.Api;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Infrastructure.Settings;
using DentalClinic.Tests.Common.Security;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Testcontainers.MsSql;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Common;

public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
{
    #pragma warning disable CS0618 // Disable obsolete warning for WebApplicationFactory
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    #pragma warning restore CS0618 // Re-enable obsolete warning for WebApplicationFactory

    /// <summary>
    /// Shared test identity used by all mediator scopes. Defaults to <c>Admin</c>;
    /// individual tests may switch it with the <c>As...</c> helpers.
    /// </summary>
    public TestCurrentUser CurrentUser { get; } = new();

    public IMediator CreateMediator()
    {
        var serviceScope = Services.CreateScope();

        return serviceScope.ServiceProvider.GetRequiredService<IMediator>();
    }

    public IAppDbContext CreateAppDbContext()
    {
        var serviceScope = Services.CreateScope();

        return serviceScope.ServiceProvider.GetRequiredService<IAppDbContext>();
    }

    public Task InitializeAsync()
    {
        return _dbContainer.StartAsync()
          .ContinueWith(async _ =>
          {
              using var scope = Services.CreateScope();
              var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

              await context.SaveChangesAsync();
          }).Unwrap();
    }

    public new Task DisposeAsync() => _dbContainer.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // WebApplicationFactory does not set ASPNETCORE_ENVIRONMENT, so the host
        // boots as Production and the API's fail-fast JWT guard would throw.
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = TestConfiguration.JwtSecret,
                ["AppSettings:RateLimiting:AuthPermitLimit"] = TestConfiguration.AuthPermitLimit.ToString(),
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(CurrentUser);

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            services.RemoveAll<AppSettings>();

            // Explicit override AFTER Configure
            services.PostConfigure<AppSettings>(opts =>
            {
                opts.OpeningTime = new TimeOnly(9, 0);
                opts.ClosingTime = new TimeOnly(18, 0);
            });
        });
    }
}