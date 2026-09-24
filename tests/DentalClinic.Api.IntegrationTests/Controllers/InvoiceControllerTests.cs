using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Invoices;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
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
    // Private Helpers
    // -------------------------------------------------------------------------

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