using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Appointments;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AppointmentsControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    private async Task EnsureUserExistsAsync()
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByIdAsync(TestUsers.Admin.Id);
        if (user is null)
        {
            await userManager.CreateAsync(TestUsers.Admin);
        }
    }

    // -------------------------------------------------------------------------
    // 1. GET /api/v1.0/appointments
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAppointments_WithValidPagination_ShouldReturnPaginatedList()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/appointments?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
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
    public async Task GetAppointments_WithInvalidPagination_ShouldReturnBadRequest(int pageNumber, int pageSize)
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync($"/api/v1.0/appointments?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointments_WithFilters_ShouldApplyFiltersCorrectly()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var queryString = $"pageNumber=1&pageSize=10&patientId={patientId}&doctorId={doctorId}&serviceId={serviceId}";

        // Act
        var response = await _client.GetAsync($"/api/v1.0/appointments?{queryString}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAppointments_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/appointments?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. GET /api/v1.0/appointments/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAppointmentById_WithValidId_ShouldReturnAppointment()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.GetAsync($"/api/v1.0/appointments/{appointment.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(result);
        Assert.Equal(appointment.Id, result!.Id);
    }

    [Fact]
    public async Task GetAppointmentById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/appointments/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointmentById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var appointmentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1.0/appointments/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. POST /api/v1.0/appointments
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAppointment_WithValidRequest_ShouldCreateAppointment()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            await context.SaveChangesAsync(default);
        });

        var request = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ServiceId = service.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(2),
            DurationInMinutes = 45,
            Notes = "Routine Checkup"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdDto = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(createdDto);
        Assert.Equal(patient.Id, createdDto!.PatientId);
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var request = new CreateAppointmentRequest
        {
            PatientId = Guid.Empty,
            DoctorId = Guid.Empty,
            ServiceId = Guid.Empty,
            ScheduledDateTime = default,
            DurationInMinutes = -10,
            Notes = null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateAppointmentRequest
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationInMinutes = 30
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 4. PUT /api/v1.0/appointments/{id}/reschedule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RescheduleAppointment_WithValidRequest_ShouldRescheduleAppointment()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        var request = new RescheduleAppointmentRequest
        {
            NewScheduledDateTime = DateTime.UtcNow.AddDays(3),
            DurationInMinutes = 60
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/appointments/{appointment.Id}/reschedule", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RescheduleAppointment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();
        var request = new RescheduleAppointmentRequest
        {
            NewScheduledDateTime = DateTime.UtcNow.AddDays(2),
            DurationInMinutes = 45
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/appointments/{nonExistentId}/reschedule", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 5. PATCH /api/v1.0/appointments/{id}/complete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteAppointment_WithValidId_ShouldUpdateStatus()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{appointment.Id}/complete", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteAppointment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{nonExistentId}/complete", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 6. PATCH /api/v1.0/appointments/{id}/cancel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CancelAppointment_WithValidId_ShouldCancelStatus()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        // Act
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{appointment.Id}/cancel", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CancelAppointment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{nonExistentId}/cancel", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var appointmentId = Guid.NewGuid();
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{appointmentId}/cancel", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}