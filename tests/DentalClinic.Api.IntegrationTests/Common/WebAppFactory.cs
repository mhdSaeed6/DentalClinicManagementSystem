// using DentalClinic.Api;
// using DentalClinic.Api.IntegrationTests.Common;
// using DentalClinic.Application.Common.Interfaces;
// using DentalClinic.Infrastructure.Data;
// using DentalClinic.Infrastructure.Identity;
// using DentalClinic.Infrastructure.Settings;
// using DentalClinic.Tests.Common.Security;

// using MediatR;

// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.AspNetCore.TestHost;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Diagnostics;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection.Extensions;
// using Microsoft.Extensions.Hosting;

// using Testcontainers.MsSql;

// using Xunit;

// namespace DentalClinic.Application.SubcutaneousTests.Common;

// public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
// {
//     private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
//     .Build();

//     public AppHttpClient CreateAppHttpClient()
//     {
//         return new AppHttpClient(CreateClient());
//     }

//     public IMediator CreateMediator()
//     {
//         var serviceScope = Services.CreateScope();

//         return serviceScope.ServiceProvider.GetRequiredService<IMediator>();
//     }

//     public IAppDbContext CreateAppDbContext()
//     {
//         var serviceScope = Services.CreateScope();

//         return serviceScope.ServiceProvider.GetRequiredService<IAppDbContext>();
//     }

//     public async Task InitializeAsync()
//     {
//         // 1. تشغيل الحاوية
//         await _dbContainer.StartAsync();

//         // 2. إنشاء الجداول وزراعة المستخدم الأساسي
//         using var scope = Services.CreateScope();
//         var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         await context.Database.EnsureCreatedAsync();

//         var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
//         var admin = TestUsers.Admin;

//         if (await userManager.FindByIdAsync(admin.Id) is null)
//         {
//             await userManager.CreateAsync(admin);
//         }
//     }

//     public new Task DisposeAsync() => _dbContainer.StopAsync();

//     protected override void ConfigureWebHost(IWebHostBuilder builder)
//     {
//         builder.ConfigureTestServices(services =>
//         {
//             services.RemoveAll<IHostedService>();

//             services.RemoveAll<DbContextOptions<AppDbContext>>();

//             services.AddDbContext<AppDbContext>((sp, options) =>
//             {
//                 options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
//                 options.UseSqlServer(_dbContainer.GetConnectionString());
//             });
//         });
//     }
// }

using System.Data.Common;
using DentalClinic.Api;
using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Respawn;

using Testcontainers.MsSql;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Common;

public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();
    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public AppHttpClient CreateAppHttpClient()
    {
        return new AppHttpClient(CreateClient());
    }

    public IMediator CreateMediator()
    {
        var serviceScope = Services.CreateScope();

        return serviceScope.ServiceProvider.GetRequiredService<IMediator>();
    }

    /// <summary>
    /// دالة مساعدة لتنفيذ أي عملية على الـ DbContext بـ Scope مستقل يغلق تلقائياً
    /// </summary>
    public async Task ExecuteDbContextAsync(Func<IAppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        await action(context);
    }

    /// <summary>
    /// دالة مساعدة لاسترجاع نتيجة من الـ DbContext بـ Scope مستقل
    /// </summary>
    public async Task<TResult> ExecuteDbContextAsync<TResult>(Func<IAppDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        return await action(context);
    }

    public async Task InitializeAsync()
    {
        // 1. تشغيل الحاوية
        await _dbContainer.StartAsync();

        // 2. إنشاء DbConnection خاص بـ Respawn وتوسيعه
        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        // 3. إنشاء الجداول وزراعة المستخدم الأساسي
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = TestUsers.Admin;

            if (await userManager.FindByIdAsync(admin.Id) is null)
            {
                await userManager.CreateAsync(admin);
            }
        }

        // 4. تهيئة Respawner باستخدام الـ Connection والـ DbAdapter الصحيح
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                "__EFMigrationsHistory",
                "AspNetRoles",
                "AspNetUserRoles",
                "AspNetUsers"
            },
            WithReseed = true
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection is not null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        await _dbContainer.StopAsync();
    }

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
        });
    }
}