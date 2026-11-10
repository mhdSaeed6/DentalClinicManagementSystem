using System.Text.Json;

using DentalClinic.Infrastructure.Data;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Scalar.AspNetCore;

using Serilog;

const string MigrateFlag = "--migrate";
const string BootstrapAdminFlag = "--bootstrap-admin";

// One-shot bootstrap modes. `dotnet DentalClinic.Api.dll --migrate` applies the
// EF migrations and seeds system data (roles + clinic profile); `--bootstrap-admin`
// additionally creates the first administrator. Both exit without starting the
// web host, which is how the production compose file provisions a fresh database.
var runMigrate = args.Any(arg => string.Equals(arg, MigrateFlag, StringComparison.OrdinalIgnoreCase));
var runBootstrapAdmin = args.Any(arg => string.Equals(arg, BootstrapAdminFlag, StringComparison.OrdinalIgnoreCase));

// The flags are ours, not host settings; keep the command-line configuration
// provider from interpreting them.
var hostArgs = args
    .Where(arg => !string.Equals(arg, MigrateFlag, StringComparison.OrdinalIgnoreCase)
                  && !string.Equals(arg, BootstrapAdminFlag, StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = WebApplication.CreateBuilder(hostArgs);

builder.Services.AddPresentation(builder.Configuration)
                .AddApplication()
                .AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (runMigrate || runBootstrapAdmin)
{
    return await RunBootstrapCommandAsync(app, bootstrapAdmin: runBootstrapAdmin);
}

// Behind a reverse proxy that terminates TLS (Caddy/Nginx/Traefik), the request
// scheme and client address arrive in X-Forwarded-Proto/X-Forwarded-For. This
// must run before HTTPS redirection and HSTS so they see the original scheme.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "DentalClinic API V1");

        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
    });

    app.MapScalarApiReference();

    await app.InitialiseDatabaseAsync();
}
else
{
    app.UseHsts();
}

app.UseCoreMiddlewares(builder.Configuration);

app.MapControllers();

// Health endpoints are mapped in every environment (unlike OpenAPI/Scalar) so
// orchestrators and load balancers can probe liveness and DB readiness in prod.
app.MapHealthChecks("/health", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Name == "database",
    ResponseWriter = WriteHealthResponseAsync,
});

app.Run();

return 0;

/// <summary>
/// Runs one of the one-shot bootstrap modes and returns the process exit code.
/// Migrations are always applied first, so <c>--bootstrap-admin</c> works
/// against a brand-new database even when the compose <c>migrate</c> service
/// was skipped.
/// </summary>
static async Task<int> RunBootstrapCommandAsync(WebApplication app, bool bootstrapAdmin)
{
    using var scope = app.Services.CreateScope();

    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    try
    {
        await initialiser.InitialiseAsync();
        await initialiser.SeedSystemDataAsync();

        if (!bootstrapAdmin)
        {
            Console.WriteLine("Migrations applied and system data seeded.");
            return 0;
        }

        await initialiser.BootstrapAdminAsync(BootstrapOptions.FromConfiguration(configuration));

        Console.WriteLine("Bootstrap administrator ensured and system data seeded.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Bootstrap command failed: {ex.Message}");
        Log.Fatal(ex, "Bootstrap command failed");
        return 1;
    }
    finally
    {
        await Log.CloseAndFlushAsync();
    }
}

/// <summary>
/// Emits a per-check breakdown so a failing probe identifies which dependency
/// is unhealthy instead of only reporting a status code.
/// </summary>
static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = new
    {
        status = report.Status.ToString(),
        durationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            error = e.Value.Exception?.Message,
        }),
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}