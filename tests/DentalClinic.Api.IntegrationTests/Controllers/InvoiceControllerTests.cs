using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Invoices;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class InvoiceControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/invoices
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetInvoices_WithValidPagination_ShouldReturnPaginatedList()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<InvoiceDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetInvoices_WithInvalidPagination_ShouldReturnBadRequest(int pageNumber, int pageSize)
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync($"/api/v1.0/invoices?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/invoices?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. GET /api/v1.0/invoices/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetInvoiceById_WithValidId_ShouldReturnInvoice()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var invoice = InvoiceTestDataBuilder.Create().ForPatient(patient.Id).Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync($"/api/v1.0/invoices/{invoice.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result!.Id);
    }

    [Fact]
    public async Task GetInvoiceById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/invoices/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoiceById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var invoiceId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1.0/invoices/{invoiceId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. POST /api/v1.0/invoices
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateInvoice_WithValidRequest_ShouldCreateInvoice()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync(default);
        });

        var request = new CreateInvoiceRequest
        {
            PatientId = patient.Id,
            TotalAmount = 500.00m,
            AppointmentId = null // <--- استخدام null بدلاً من Guid.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdDto = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(createdDto);
        Assert.Equal(request.PatientId, createdDto!.PatientId);
        Assert.Equal(request.TotalAmount, createdDto.TotalAmount);
    }

    [Fact]
    public async Task CreateInvoice_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateInvoiceRequest
        {
            PatientId = Guid.Empty, // معرف مريض غير صالحة
            TotalAmount = -100.00m, // مبلغ بالسالب غير مقبول
            AppointmentId = Guid.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            PatientId = Guid.NewGuid(),
            TotalAmount = 200.00m,
            AppointmentId = Guid.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 4. POST /api/v1.0/invoices/{invoiceId}/payments
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddPayment_WithValidRequest_ShouldAddPaymentToInvoice()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var invoice = InvoiceTestDataBuilder.Create()
            .ForPatient(patient.Id)
            .WithAmount(500.00m)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(default);
        });

        var request = new AddInvoicePaymentRequest
        {
            Amount = 200.00m,
            Notes = "Partial cash payment"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1.0/invoices/{invoice.Id}/payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedInvoice = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(updatedInvoice);
        Assert.Equal(invoice.Id, updatedInvoice!.Id);
    }

    [Fact]
    public async Task AddPayment_WithInvalidInvoiceId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();
        var request = new AddInvoicePaymentRequest
        {
            Amount = 100.00m,
            Notes = "Payment test"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1.0/invoices/{nonExistentId}/payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 5. POST /api/v1.0/invoices/from-appointment/{appointmentId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GenerateFromAppointment_WithServiceAndTreatments_ShouldCreateLinkedDraft()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var (patient, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);
        await SeedTreatmentAsync(appointment, 36, "Extraction", 100m);

        // Act
        var response = await PostGenerateAsync(appointment.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        Assert.NotNull(result);
        Assert.False(result!.ReusedExisting);
        Assert.Equal(40m, result.ServiceSubtotal);
        Assert.Equal(160m, result.TreatmentSubtotal);
        Assert.Equal(200m, result.TotalAmount);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(appointment.Id, result.AppointmentId);
        Assert.Equal(3, result.Lines.Count);

        var persisted = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == result.InvoiceId));

        Assert.NotNull(persisted);
        Assert.Equal(appointment.Id, persisted!.AppointmentId);
        Assert.Equal(patient.Id, persisted.PatientId);
        Assert.Equal(200m, persisted.TotalAmount);
    }

    [Fact]
    public async Task GenerateFromAppointment_ThenFilterByAppointment_ShouldReturnLinkedInvoice()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generate = await PostGenerateAsync(appointment.Id);
        Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
        var created = await generate.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Act
        var listResponse = await _client.GetAsync(
            $"/api/v1.0/invoices?pageNumber=1&pageSize=10&appointmentId={appointment.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedList<InvoiceDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!.Items!, i => i.Id == created!.InvoiceId);
        Assert.All(list.Items!, i => Assert.Equal(appointment.Id, i.AppointmentId));
    }

    [Fact]
    public async Task GenerateFromAppointment_WhenRepeated_ShouldReturnExistingDraftWithoutDuplicating()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        // Act
        var first = await PostGenerateAsync(appointment.Id);
        var second = await PostGenerateAsync(appointment.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstResult = await first.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var secondResult = await second.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        Assert.False(firstResult!.ReusedExisting);
        Assert.True(secondResult!.ReusedExisting);
        Assert.Equal(firstResult.InvoiceId, secondResult.InvoiceId);

        var count = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Invoices.AsNoTracking().CountAsync(i => i.AppointmentId == appointment.Id));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GenerateFromAppointment_WithUnknownAppointment_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await PostGenerateAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateFromAppointment_ConsultationOnly_ShouldReturnBadRequestAndCreateNothing()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: null);

        // Act
        var response = await PostGenerateAsync(appointment.Id);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var count = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Invoices.AsNoTracking().CountAsync(i => i.AppointmentId == appointment.Id));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GenerateFromAppointment_AsUnrelatedDoctor_ShouldReturnForbidden()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var (doctorEmail, _) = await EnsureDoctorUserAsync("invoice.authz.doctor@dentalclinic.com");
        var token = await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword);
        _client.SetAuthorizationHeader(token);

        // The appointment is assigned to a different doctor than the caller.
        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        // Act
        var response = await PostGenerateAsync(appointment.Id);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateFromAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.ClearAuthorizationHeader();

        // Act
        var response = await PostGenerateAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 6. Invoice lifecycle: draft -> issued, editing, refresh, payment
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GeneratedDraft_ShouldStartAsDraftWithPersistedLinesAndIssuedTotal()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        // Act
        var generate = await PostGenerateAsync(appointment.Id);
        Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
        var generated = await generate.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Assert
        Assert.Equal(InvoiceLifecycleStatus.Draft, generated!.LifecycleStatus);

        var detail = await GetInvoiceAsync(generated.InvoiceId);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var invoice = await detail.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(InvoiceLifecycleStatus.Draft, invoice!.LifecycleStatus);
        Assert.Equal(PaymentStatus.Unpaid, invoice.Status);

        // The breakdown must still be reviewable after the request completes.
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal(100m, invoice.Lines.Sum(l => l.Amount));
        Assert.Equal(100m, invoice.TotalAmount);
    }

    [Fact]
    public async Task Payment_AgainstDraft_ShouldBeRejected()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Act - a draft must not accept money before it is issued.
        var pay = new AddInvoicePaymentRequest { Amount = 10m, Notes = "should fail" };
        var response = await _client.PostAsJsonAsync($"/api/v1.0/invoices/{generated!.InvoiceId}/payments", pay);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var unchanged = await (await GetInvoiceAsync(generated.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(0m, unchanged!.PaidAmount);
        Assert.Equal(PaymentStatus.Unpaid, unchanged.Status);
    }

    [Fact]
    public async Task Issue_Draft_ShouldBecomeIssuedAndRemainUnpaid()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Act
        var response = await PostIssueAsync(generated!.InvoiceId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issued = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        Assert.Equal(InvoiceLifecycleStatus.Issued, issued!.LifecycleStatus);
        Assert.Equal(100m, issued.TotalAmount);

        // Issuance must never register a payment.
        Assert.Equal(0m, issued.PaidAmount);
        Assert.Equal(PaymentStatus.Unpaid, issued.Status);
    }

    [Fact]
    public async Task Issue_WhenRepeated_ShouldBeIdempotent()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Act
        var first = await PostIssueAsync(generated!.InvoiceId);
        var second = await PostIssueAsync(generated.InvoiceId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var issued = await second.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(InvoiceLifecycleStatus.Issued, issued!.LifecycleStatus);
        Assert.Equal(issued.TotalAmount, issued.PaidAmount + issued.RemainingAmount);
    }

    [Fact]
    public async Task Issue_ThenPayment_ShouldSucceed()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        await PostIssueAsync(generated!.InvoiceId);

        // Act
        var pay = new AddInvoicePaymentRequest { Amount = 15m };
        var response = await _client.PostAsJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/payments", pay);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(15m, updated!.PaidAmount);
        Assert.Equal(PaymentStatus.PartiallyPaid, updated.Status);
    }

    [Fact]
    public async Task UpdateDraft_ShouldRecalculateTotalOnServer()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var draft = await (await GetInvoiceAsync(generated!.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        var draftLines = draft!.Lines.ToList();

        // Act - the client sends lines only; it never supplies a total.
        var request = new
        {
            lines = new[]
            {
                new { lineId = draftLines[0].Id, description = "Teeth Cleaning", amount = 50m },
                new { lineId = draftLines[1].Id, description = "Composite filling", amount = 75m },
            }
        };

        var response = await PutJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/draft", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        Assert.Equal(125m, updated!.TotalAmount);
        Assert.Equal(2, updated.Lines.Count);
        Assert.Equal(InvoiceLifecycleStatus.Draft, updated.LifecycleStatus);
    }

    [Fact]
    public async Task UpdateDraft_OnIssuedInvoice_ShouldBeRejected()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var draft = await (await GetInvoiceAsync(generated!.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        var draftLines = draft!.Lines.ToList();

        await PostIssueAsync(generated.InvoiceId);

        // Act - editing after issuance must be refused.
        var request = new
        {
            lines = new[]
            {
                new { lineId = draftLines[0].Id, description = "Teeth Cleaning", amount = 999m },
            }
        };

        var response = await PutJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/draft", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await (await GetInvoiceAsync(generated.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(draftLines[0].Amount, unchanged!.Lines.First().Amount);
        Assert.Equal(draft.TotalAmount, unchanged.TotalAmount);
    }

    [Fact]
    public async Task UpdateDraft_OnIssuedInvoice_WithPartialLineSet_ShouldReportConflictNotNotFound()
    {
        // Regression guard: the invoice state must be validated before the line
        // set, otherwise editing an issued invoice reports a misleading 404
        // when the client submits fewer lines than the invoice has.
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var draft = await (await GetInvoiceAsync(generated!.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        var draftLines = draft!.Lines.ToList();

        Assert.Equal(2, draftLines.Count);
        await PostIssueAsync(generated.InvoiceId);

        // Deliberately send only one of the two lines.
        var response = await PutJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/draft", new
        {
            lines = new[]
            {
                new { lineId = draftLines[0].Id, description = "Teeth Cleaning", amount = 1m },
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await (await GetInvoiceAsync(generated.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(InvoiceLifecycleStatus.Issued, unchanged!.LifecycleStatus);
        Assert.Equal(draft.TotalAmount, unchanged.TotalAmount);
    }

    [Fact]
    public async Task UpdateDraft_WithPartialLineSet_ShouldRemoveTheOmittedLine()
    {
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var draft = await (await GetInvoiceAsync(generated!.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        var draftLines = draft!.Lines.ToList();

        // Keep only the service line.
        var response = await PutJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/draft", new
        {
            lines = new[]
            {
                new { lineId = draftLines[0].Id, description = "Teeth Cleaning", amount = 40m },
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        Assert.Single(updated!.Lines);
        Assert.Equal(40m, updated.TotalAmount);
    }

    [Fact]
    public async Task RefreshDraft_WithManualAdjustments_ShouldRequireConfirmation()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        var draft = await (await GetInvoiceAsync(generated!.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        var draftLines = draft!.Lines.ToList();

        // Introduce a manual adjustment.
        await PutJsonAsync($"/api/v1.0/invoices/{generated.InvoiceId}/draft", new
        {
            lines = new[]
            {
                new { lineId = draftLines[0].Id, description = "Teeth Cleaning (negotiated)", amount = 25m },
                new { lineId = draftLines[1].Id, description = "Composite filling", amount = 60m },
            }
        });

        // Act - refreshing without confirmation must not discard the manual work.
        var refused = await PostRefreshAsync(generated.InvoiceId, confirm: false);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var preserved = await (await GetInvoiceAsync(generated.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(85m, preserved!.TotalAmount);
        Assert.Contains(preserved.Lines, l => l.Description == "Teeth Cleaning (negotiated)");

        // Act - confirmed refresh recalculates from the appointment.
        var confirmed = await PostRefreshAsync(generated.InvoiceId, confirm: true);

        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        var refreshed = await confirmed.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(100m, refreshed!.TotalAmount);
        Assert.DoesNotContain(refreshed.Lines, l => l.Description == "Teeth Cleaning (negotiated)");
    }

    [Fact]
    public async Task RefreshDraft_WithoutManualAdjustments_ShouldSucceedImmediately()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        await SeedTreatmentAsync(appointment, 11, "Composite filling", 60m);

        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();

        // Act
        var response = await PostRefreshAsync(generated!.InvoiceId, confirm: false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(100m, refreshed!.TotalAmount);
    }

    [Fact]
    public async Task Generate_ForAppointmentThatAlreadyHasIssuedInvoice_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();
        await AuthenticateAdminAsync();

        var (_, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 40m);
        var generated = await (await PostGenerateAsync(appointment.Id))
            .Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        await PostIssueAsync(generated!.InvoiceId);

        // Act
        var response = await PostGenerateAsync(appointment.Id);

        // Assert - an issued invoice is never regenerated or downgraded.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await (await GetInvoiceAsync(generated.InvoiceId))
            .Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(InvoiceLifecycleStatus.Issued, unchanged!.LifecycleStatus);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private Task AuthenticateAdminAsync()
    {
        EnsureUserExistsAsync().GetAwaiter().GetResult();
        var token = _client.GenerateTokenAsync(TestUsers.Admin).GetAwaiter().GetResult();
        _client.SetAuthorizationHeader(token);
        return Task.CompletedTask;
    }

    private Task<HttpResponseMessage> GetInvoiceAsync(Guid invoiceId) =>
        _client.GetAsync($"/api/v1.0/invoices/{invoiceId}");

    private Task<HttpResponseMessage> PostIssueAsync(Guid invoiceId) =>
        _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/invoices/{invoiceId}/issue"));

    private Task<HttpResponseMessage> PostRefreshAsync(Guid invoiceId, bool confirm) =>
        _client.PostAsJsonAsync(
            $"/api/v1.0/invoices/{invoiceId}/draft/refresh",
            new { confirmDiscardManualAdjustments = confirm });

    private Task<HttpResponseMessage> PutJsonAsync<T>(string url, T body) =>
        _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body),
        });

    private Task<HttpResponseMessage> PostGenerateAsync(Guid appointmentId) =>
        _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/invoices/from-appointment/{appointmentId}"));

    private async Task<(Patient Patient, Doctor Doctor, Appointment Appointment)>
        SeedCompletedAppointmentAsync(decimal? servicePrice)
    {
        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = servicePrice is { } price
            ? DentalServiceTestDataBuilder.Create().WithCost(price).Build()
            : null;

        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            service?.Id,
            DateTime.UtcNow.AddHours(1),
            30).Value;

        appointment.Complete();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            if (service is not null)
            {
                context.DentalServices.Add(service);
            }

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        return (patient, doctor, appointment);
    }

    private async Task SeedTreatmentAsync(
        Appointment appointment,
        int toothNumber,
        string procedure,
        decimal cost)
    {
        var treatment = TreatmentRecord.Create(
            appointment.PatientId,
            appointment.DoctorId,
            toothNumber,
            procedure,
            cost,
            appointment.Id).Value;

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.TreatmentRecords.Add(treatment);
            await context.SaveChangesAsync(default);
        });
    }

    private async Task<(string Email, Doctor Doctor)> EnsureDoctorUserAsync(string email)
    {
        AppUser user;
        using (var scope = webAppFactory.Services.CreateScope())
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
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            doctor = DoctorTestDataBuilder.Create().WithName("Invoice", "Authz").Build();
            doctor.AssignUser(user.Id);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (email, doctor);
    }

    private async Task EnsureUserExistsAsync()
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByIdAsync(TestUsers.Admin.Id);
        if (user is null)
        {
            await userManager.CreateAsync(TestUsers.Admin, TestUsers.AdminPassword);
        }
    }
}