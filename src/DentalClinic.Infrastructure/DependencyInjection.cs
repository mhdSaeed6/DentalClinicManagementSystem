using System.Text;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Data;
using DentalClinic.Infrastructure.Data.Interceptors;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Infrastructure.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentNullException.ThrowIfNull(connectionString);

        // 1. Interceptors & DbContext Setup
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SoftDeleteInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseSqlServer(connectionString, sql =>
            {
                // Split queries by default. Loading two collection navigations in a
                // single query cartesian-joins them, which duplicates tracked
                // entities and corrupts change tracking on save (e.g. an invoice
                // with both Lines and Payments).
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // ربط AppSettings بالـ Configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // 2. Initialiser / Seeder
        services.AddScoped<ApplicationDbContextInitialiser>();

        // 3. Identity Core Setup
        services.AddIdentityCore<AppUser>(options =>
        {
            // Password policy applies to every identity operation, including the
            // test fixtures. Keep the seeded/tested credentials in sync with
            // Tests.Common/Security/TestUsers.cs when changing any rule here.
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredUniqueChars = 4;

            // Brute-force protection: without a lockout the sliding-window
            // limiter alone cannot stop credential stuffing.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>();

        // 4. JWT Authentication Setup
        // The signing secret is deliberately absent from appsettings.json. A
        // committed secret lets anyone with repo access mint an Admin token, so
        // it must come from user-secrets, an env var, or a secret store.
        //
        // The secret is resolved LAZILY (see JwtSecretProvider): reading it here
        // during registration would capture configuration before the host is
        // built, missing anything a test host or late configuration source adds.
        // The provider is constructed here over the same live IConfiguration so
        // it observes configuration sources registered later.
        var jwtSecretProvider = new JwtSecretProvider(configuration);

        services.AddSingleton<IJwtSecretProvider>(jwtSecretProvider);

        // Fail fast at startup rather than on the first authenticated request.
        services.AddHostedService<JwtSecretValidationService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // AddJwtBearer's action is applied lazily via services.Configure, so
            // reading configuration here (rather than at registration time) sees
            // the final, fully-built configuration.
            var jwtSettings = configuration.GetSection("JwtSettings");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretProvider.GetSecret())),
            };
        });

        // 5. Authorization Policies
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor"))
            .AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Doctor", "Receptionist"))
            .AddPolicy("ClinicalWrite", policy => policy.RequireRole("Admin", "Doctor"));

        // 6. Identity Services & Token Provider
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddScoped<ITokenProvider, TokenProvider>();

        // 7. Caching (HybridCache)
        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.FromSeconds(30),
        });

        return services;
    }
}