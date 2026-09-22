using DentalClinic.Api;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Infrastructure.Settings;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

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