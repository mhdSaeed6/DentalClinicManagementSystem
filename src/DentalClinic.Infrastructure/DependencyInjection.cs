using System.Security.Cryptography;
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
        var jwtSettings = configuration.GetSection("JwtSettings");
        var jwtSecret = ResolveJwtSecret(configuration, jwtSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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

    /// <summary>
    /// Resolves the JWT signing secret. Outside Development the secret must be
    /// supplied explicitly (env var <c>JwtSettings__Secret</c>, user-secrets, or
    /// a vault); the process refuses to start without it. In Development an
    /// ephemeral in-memory secret is generated so a fresh clone runs without
    /// setup, and so no usable secret ever lives in git.
    /// </summary>
    private static string ResolveJwtSecret(IConfiguration configuration, IConfigurationSection jwtSettings)
    {
        var configured = jwtSettings["Secret"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. Set it via the JwtSettings__Secret " +
                "environment variable, user-secrets, or your secret manager. Refusing to start.");
        }

        var developmentSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        // Back-fill so TokenProvider reads the identical value.
        configuration["JwtSettings:Secret"] = developmentSecret;

        return developmentSecret;
    }
}