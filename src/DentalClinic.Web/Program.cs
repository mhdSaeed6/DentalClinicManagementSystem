using DentalClinic.Web.Api;
using DentalClinic.Web.Components;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Data Protection keys back the antiforgery tokens used by every interactive
// form. Holding them in memory meant a container restart invalidated all of
// them. Persist the key ring on the volume the compose files mount, under the
// same application name as the API so both apps share one ring. Inside the
// image the content root is /app, i.e. /app/App_Data/keys; `dotnet run` gets a
// project-local folder instead of a container path.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];

if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
}

builder.Services.AddDataProtection()
    .SetApplicationName("DentalClinic")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Behind a reverse proxy that terminates TLS (Caddy/Nginx/Traefik), the request
// scheme and client address arrive in X-Forwarded-Proto/X-Forwarded-For.
// Without this, HTTPS redirection and Blazor Server's WebSocket URL generation
// see the proxy's plain-HTTP hop instead of the public HTTPS request.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<ClinicSessionState>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ClinicBranding>();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5004");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    if (builder.Environment.IsDevelopment())
    {
        // The API is served behind a development (self-signed) certificate.
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

// Branding reads the profile through the same HTTP client (the endpoint is
// anonymous), so there is exactly one place that knows the API base address
// and the development certificate policy.
builder.Services.AddScoped<IClinicSettingsSource>(sp => sp.GetRequiredService<ApiClient>());

var app = builder.Build();

// Must run before anything that reads the scheme, host or client address.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Container/orchestrator liveness probe. It deliberately does not call the API:
// a slow backend must not restart a healthy web process. The API exposes its
// own /health/ready probe for database readiness.
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();