using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Features.Reports.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

/// <summary>
/// The dashboard/report aggregate endpoint. These tests deliberately avoid
/// asserting a specific clinic day bucket - the seeded timezone differs between
/// developer machines and CI - and instead pin the questions an owner asks:
/// what was billed in the window, what is owed on the books, and whose figures
/// a doctor is allowed to see.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class ReportsControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public ReportsControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboardSummary_ShouldAggregateServerSide_ForTheDefaultWindow()
    {
        // Arrange - a recent invoice (inside the default window) and an old one
        // (outside it). Both are owed, so the balance on the books must include
        // both while revenue only counts the recent one.
        var patient = PatientTestDataBuilder.Create().Build();
        var other = PatientTestDataBuilder.Create().WithName("Ada", "Cole").Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().WithCost(30m).Build();

        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            service.Id,
            DateTime.UtcNow.AddHours(2),
            30).Value;

        var recent = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(200m).Build();
        var old = NewInvoice(patient.Id, 300m, DateTimeOffset.UtcNow.AddDays(-90));

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(patient, other);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            context.Invoices.AddRange(recent, old);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync("/api/v1.0/reports/dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);

        // Register #7: omitting both dates means the clinic's last 30 days.
        Assert.Equal(summary!.To.AddDays(-29), summary.From);
        Assert.InRange(
            summary.To,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

        Assert.Equal(2, summary.TotalPatients);
        Assert.Equal(1, summary.Revenue.TotalInvoiceCount);
        Assert.Equal(200m, summary.Revenue.TotalBilled);
        Assert.Equal(500m, summary.Outstanding.TotalOutstanding);
        Assert.Equal(2, summary.Outstanding.OpenInvoiceCount);

        // Names are resolved in the same round trip, so the dashboard never has
        // to hold three lookup lists to label a handful of rows.
        var upcoming = Assert.Single(summary.UpcomingAppointments);
        Assert.Equal("John Doe", upcoming.PatientName);
        Assert.Equal($"{doctor.FirstName} {doctor.LastName}", upcoming.DoctorName);
        Assert.Equal(service.Name, upcoming.ServiceName);
    }

    [Fact]
    public async Task GetDashboardSummary_WithAnExplicitRange_ShouldReportOnlyThatWindow()
    {
        // Arrange
        var patient = PatientTestDataBuilder.Create().Build();
        var recent = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(200m).Build();
        var old = NewInvoice(patient.Id, 300m, DateTimeOffset.UtcNow.AddDays(-90));

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Invoices.AddRange(recent, old);
            await context.SaveChangesAsync(default);
        });

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-95));
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-85));

        // Act - DateOnly binds straight from the query string.
        var response = await _client.GetAsync(
            $"/api/v1.0/reports/dashboard?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(from, summary!.From);
        Assert.Equal(to, summary.To);
        Assert.Equal(1, summary.Revenue.TotalInvoiceCount);
        Assert.Equal(300m, summary.Revenue.TotalBilled);

        // The window narrows the revenue question only; debt is not aged out.
        Assert.Equal(500m, summary.Outstanding.TotalOutstanding);
    }

    [Fact]
    public async Task GetDashboardSummary_WithAWideRange_ShouldIncludeTheAppointmentMix()
    {
        // Arrange - a range wide enough that "today" never falls outside it,
        // whatever timezone the seeded clinic uses.
        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().WithCost(30m).Build();

        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            service.Id,
            DateTime.UtcNow.AddHours(2),
            30).Value;

        var invoice = InvoiceTestDataBuilder
            .Create()
            .ForPatient(patient.Id)
            .ForAppointment(appointment.Id)
            .WithAmount(150m)
            .Build();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(default);
        });

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Act
        var response = await _client.GetAsync(
            $"/api/v1.0/reports/dashboard?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);

        // Every status is returned, including the zeroes, so the UI never has
        // to invent a missing bar.
        Assert.Equal(4, summary!.AppointmentStatusBreakdown.Count);
        Assert.Equal(1, summary.AppointmentStatusBreakdown.Sum(row => row.Count));
        Assert.Equal(
            1,
            summary.AppointmentStatusBreakdown.Single(row => row.Status == AppointmentStatus.Scheduled).Count);

        var productivity = Assert.Single(summary.DoctorProductivity);
        Assert.Equal(1, productivity.AppointmentCount);
        Assert.Equal(1, productivity.IssuedInvoiceCount);
        Assert.Equal(150m, productivity.BilledAmount);
    }

    [Fact]
    public async Task GetDashboardSummary_WithAReversedRange_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/reports/dashboard?from=2030-06-15&to=2030-06-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardSummary_AsDoctor_ShouldOnlySeeTheirOwnFigures()
    {
        // Arrange - the doctor owns one appointment-linked invoice; the
        // standalone invoice and the other doctor's appointment stay out.
        var (email, doctor) = await EnsureDoctorUserAsync("reports.doctor@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var otherDoctor = DoctorTestDataBuilder.Create().WithName("Rami", "Saab").Build();
        var service = DentalServiceTestDataBuilder.Create().WithCost(30m).Build();

        var ownAppointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30).Value;
        var otherAppointment = Appointment.Create(patient.Id, otherDoctor.Id, service.Id, DateTime.UtcNow.AddHours(3), 30).Value;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(otherDoctor);
            context.DentalServices.Add(service);
            context.Appointments.AddRange(ownAppointment, otherAppointment);
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).ForAppointment(ownAppointment.Id).WithAmount(120m).Build());
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).ForAppointment(otherAppointment.Id).WithAmount(340m).Build());
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(700m).Build());
            await context.SaveChangesAsync(default);
        });

        var token = await _client.GenerateTokenAsync(email, TestUsers.DoctorPassword);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/reports/dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(120m, summary!.Revenue.TotalBilled);

        var productivity = Assert.Single(summary.DoctorProductivity);
        Assert.Equal(doctor.Id, productivity.DoctorId);
        Assert.Equal(120m, productivity.BilledAmount);
    }

    [Fact]
    public async Task GetDashboardSummary_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.ClearAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/v1.0/reports/dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Invoice NewInvoice(Guid patientId, decimal totalAmount, DateTimeOffset issuedAt) =>
        Invoice.Create(
            patientId,
            totalAmount,
            appointmentId: null,
            InvoiceLifecycleStatus.Issued,
            $"INV-OLD-{Guid.NewGuid():N}"[..21],
            issuedAt).Value;

    private async Task<(string Email, Doctor Doctor)> EnsureDoctorUserAsync(string email)
    {
        AppUser user;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            user = (await userManager.FindByEmailAsync(email))!;
            if (user is null)
            {
                user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
                var created = await userManager.CreateAsync(user, TestUsers.DoctorPassword);
                Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, "Doctor"))
            {
                await userManager.AddToRoleAsync(user, "Doctor");
            }
        }

        Doctor doctor = null!;
        await _factory.ExecuteDbContextAsync(async context =>
        {
            doctor = DoctorTestDataBuilder.Create().WithName("Reports", "DoctorTest").Build();
            doctor.AssignUser(user.Id);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (email, doctor);
    }
}