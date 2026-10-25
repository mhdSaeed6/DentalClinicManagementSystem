using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
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
/// Server-side money aggregation over issued invoices (summary + debtor list).
/// Every test starts from a reset database; cached-query keys stay distinct
/// because each scenario sends different query parameters.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class InvoiceBillingReportControllerTests : IAsyncLifetime
{
    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public InvoiceBillingReportControllerTests(WebAppFactory factory)
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

    // -------------------------------------------------------------------------
    // GET /api/v1.0/invoices/summary
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSummary_OverIssuedInvoices_ShouldReturnTotalsAndExcludeDrafts()
    {
        // Arrange — two debtors: 200 + 150(50 paid) + a fully paid 100, and a
        // draft that must never influence the money totals.
        var patient = PatientTestDataBuilder.Create().Build();

        var unpaid = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(200m).Build();
        var partial = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(150m).Build();
        partial.RegisterPayment(50m);
        var paid = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(100m).Paid().Build();
        var draft = Invoice.Create(patient.Id, 175m, null, InvoiceLifecycleStatus.Draft).Value;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Invoices.AddRange(unpaid, partial, paid, draft);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<InvoiceSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(3, summary!.TotalInvoiceCount);
        Assert.Equal(450m, summary.TotalBilled);
        Assert.Equal(150m, summary.TotalPaid);
        Assert.Equal(300m, summary.TotalOutstanding);
        Assert.Equal(2, summary.OpenInvoiceCount);
        Assert.NotNull(summary.OldestUnpaidInvoiceDateUtc);
    }

    [Fact]
    public async Task GetSummary_WithPatientFilter_ShouldReturnOnlyThatPatientsBalance()
    {
        // Arrange
        var debtor = PatientTestDataBuilder.Create().Build();
        var other = PatientTestDataBuilder.Create().WithName("Ada", "Cole").Build();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(debtor, other);
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(debtor.Id).WithAmount(120m).Build());
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(other.Id).WithAmount(999m).Build());
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync($"/api/v1.0/invoices/summary?patientId={debtor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<InvoiceSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary!.TotalInvoiceCount);
        Assert.Equal(120m, summary.TotalOutstanding);
    }

    [Fact]
    public async Task GetSummary_ForAPatientWithoutInvoices_ShouldReturnZeroes()
    {
        // Act — an unmatched patient id keeps its own cache key, so this holds
        // regardless of execution order.
        var response = await _client.GetAsync($"/api/v1.0/invoices/summary?patientId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<InvoiceSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(0m, summary!.TotalBilled);
        Assert.Equal(0, summary.TotalInvoiceCount);
        Assert.Null(summary.OldestUnpaidInvoiceDateUtc);
    }

    [Fact]
    public async Task GetSummary_AsDoctor_ShouldOnlySeeInvoicesLinkedToTheirAppointments()
    {
        // Arrange — an own appointment-linked invoice (120) is visible; a
        // standalone invoice and another doctor's invoice are not.
        var (email, doctor) = await EnsureDoctorUserAsync("billing.summary.doctor@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var otherDoctor = DoctorTestDataBuilder.Create().WithName("Rami", "Saab").Build();
        var service = DentalServiceTestDataBuilder.Create().WithCost(30m).Build();

        var ownAppointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30).Value;
        var otherAppointment = Appointment.Create(patient.Id, otherDoctor.Id, service.Id, DateTime.UtcNow.AddHours(2), 30).Value;

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
        var response = await _client.GetAsync("/api/v1.0/invoices/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<InvoiceSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary!.TotalInvoiceCount);
        Assert.Equal(120m, summary.TotalOutstanding);
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.ClearAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/summary");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1.0/invoices/outstanding-debts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOutstandingDebts_ShouldGroupPerPatientAndExcludePaidInvoices()
    {
        // Arrange — John owes 250 (200 unpaid + 50 left), Ada owes 80 — the
        // entirely-paid invoice and the draft leave no debt row.
        var john = PatientTestDataBuilder.Create().WithName("John", "Rew").Build();
        var ada = PatientTestDataBuilder.Create().WithName("Ada", "Cole").Build();

        var unpaid = InvoiceTestDataBuilder.Create().ForPatient(john.Id).WithAmount(200m).Build();
        var partial = InvoiceTestDataBuilder.Create().ForPatient(john.Id).WithAmount(150m).Build();
        partial.RegisterPayment(100m);
        var smallDebt = InvoiceTestDataBuilder.Create().ForPatient(ada.Id).WithAmount(80m).Build();
        var fullyPaid = InvoiceTestDataBuilder.Create().ForPatient(ada.Id).WithAmount(300m).Paid().Build();
        var draft = Invoice.Create(ada.Id, 175m, null, InvoiceLifecycleStatus.Draft).Value;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(john, ada);
            context.Invoices.AddRange(unpaid, partial, smallDebt, fullyPaid, draft);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/outstanding-debts?pageNumber=1&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var debts = await response.Content.ReadFromJsonAsync<PaginatedList<OutstandingDebtDto>>();
        Assert.NotNull(debts!.Items);
        Assert.Equal(2, debts.TotalCount);

        var rows = debts.Items!.ToList();

        // Largest debt first, and the name is resolved on the server.
        Assert.Equal(john.Id, rows[0].PatientId);
        Assert.Equal("John Rew", rows[0].PatientName);
        Assert.Equal(250m, rows[0].TotalOutstanding);
        Assert.Equal(2, rows[0].OpenInvoiceCount);
        Assert.Equal(ada.Id, rows[1].PatientId);
        Assert.Equal(80m, rows[1].TotalOutstanding);
    }

    [Fact]
    public async Task GetOutstandingDebts_WithMinAmount_ShouldFilterDebtors()
    {
        // Arrange
        var bigDebtor = PatientTestDataBuilder.Create().Build();
        var smallDebtor = PatientTestDataBuilder.Create().WithName("Sam", "Orr").Build();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(bigDebtor, smallDebtor);
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(bigDebtor.Id).WithAmount(220m).Build());
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(smallDebtor.Id).WithAmount(25m).Build());
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/outstanding-debts?pageNumber=1&pageSize=26&minAmount=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var debts = await response.Content.ReadFromJsonAsync<PaginatedList<OutstandingDebtDto>>();
        Assert.NotNull(debts!.Items);
        var row = Assert.Single(debts.Items!);
        Assert.Equal(bigDebtor.Id, row.PatientId);
    }

    [Fact]
    public async Task GetOutstandingDebts_WhenNothingIsOwed_ShouldReturnEmptyList()
    {
        // Arrange
        var patient = PatientTestDataBuilder.Create().Build();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(120m).Paid().Build());
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/outstanding-debts?pageNumber=1&pageSize=27");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var debts = await response.Content.ReadFromJsonAsync<PaginatedList<OutstandingDebtDto>>();
        Assert.NotNull(debts!.Items);
        Assert.Empty(debts.Items!);
        Assert.Equal(0, debts.TotalCount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    public async Task GetOutstandingDebts_WithInvalidPagination_ShouldReturnBadRequest(int pageNumber, int pageSize)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1.0/invoices/outstanding-debts?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOutstandingDebts_AsDoctor_ShouldHideStandaloneDebts()
    {
        // Arrange
        var (email, doctor) = await EnsureDoctorUserAsync("billing.debts.doctor@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().WithName("Reem", "Nasr").Build();
        var service = DentalServiceTestDataBuilder.Create().WithCost(30m).Build();
        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30).Value;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).ForAppointment(appointment.Id).WithAmount(90m).Build());
            context.Invoices.Add(InvoiceTestDataBuilder.Create().ForPatient(patient.Id).WithAmount(700m).Build());
            await context.SaveChangesAsync(default);
        });

        var token = await _client.GenerateTokenAsync(email, TestUsers.DoctorPassword);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/outstanding-debts?pageNumber=1&pageSize=28");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var debts = await response.Content.ReadFromJsonAsync<PaginatedList<OutstandingDebtDto>>();
        Assert.NotNull(debts!.Items);
        var onlyRow = Assert.Single(debts.Items!);
        Assert.Equal(90m, onlyRow.TotalOutstanding);
    }

    [Fact]
    public async Task GetOutstandingDebts_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.ClearAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices/outstanding-debts?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

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
            doctor = DoctorTestDataBuilder.Create().WithName("Billing", "DoctorTest").Build();
            doctor.AssignUser(user.Id);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (email, doctor);
    }
}
