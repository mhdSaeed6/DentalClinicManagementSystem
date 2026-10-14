using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AppointmentsControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

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
    public async Task CreateAppointment_WithoutService_ShouldCreateAppointment()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        var request = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ServiceId = null,
            ScheduledDateTime = DateTime.UtcNow.AddDays(2),
            DurationInMinutes = 45,
            Notes = "No service selected"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdDto = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(createdDto);
        Assert.Null(createdDto!.ServiceId);
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
    public async Task CreateAppointment_WithOverlappingSlotForSameDoctor_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        var slotStart = DateTime.UtcNow.AddDays(2);

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .ScheduledAt(slotStart)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        var request = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ServiceId = service.Id,
            ScheduledDateTime = slotStart.AddMinutes(15),
            DurationInMinutes = 45,
            Notes = null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
    public async Task RescheduleAppointment_WithOverlappingSlotForSameDoctor_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        var blockerStart = DateTime.UtcNow.AddDays(3);
        var targetStart = DateTime.UtcNow.AddDays(4);

        var blocker = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .ScheduledAt(blockerStart)
            .Build();

        var toReschedule = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .ScheduledAt(targetStart)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(service);
            context.Appointments.Add(blocker);
            context.Appointments.Add(toReschedule);
            await context.SaveChangesAsync(default);
        });

        // Move the second appointment into the blocker's slot (overlaps).
        var request = new RescheduleAppointmentRequest
        {
            NewScheduledDateTime = blockerStart.AddMinutes(10),
            DurationInMinutes = 60
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1.0/appointments/{toReschedule.Id}/reschedule", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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

        // The booked service is preserved when the caller sends no body.
        var dbAppointment = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id));
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);
        Assert.Equal(service.Id, dbAppointment.ServiceId);
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

    [Fact]
    public async Task CompleteAppointment_WithTreatments_ShouldCreateLinkedTreatmentRecords()
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

        var request = new CompleteAppointmentRequest
        {
            Treatments =
            [
                new CompleteAppointmentTreatmentRequest { ToothNumber = 11, ProcedureDetails = "Composite filling", Cost = 50m },
                new CompleteAppointmentTreatmentRequest { ToothNumber = 36, ProcedureDetails = "Extraction", Cost = 120m },
            ],
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var records = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.TreatmentRecords
                .Where(t => t.AppointmentId == appointment.Id)
                .ToListAsync());

        Assert.Equal(2, records.Count);
        Assert.All(records, record =>
        {
            Assert.Equal(appointment.PatientId, record.PatientId);
            Assert.Equal(appointment.DoctorId, record.DoctorId);
        });

        var dbAppointment = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id));

        Assert.NotNull(dbAppointment);
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);
    }

    [Fact]
    public async Task CompleteAppointment_WithDifferentService_ShouldReplaceAppointmentServiceOnly()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var patient = PatientTestDataBuilder.Create().Build();
        var doctor = DoctorTestDataBuilder.Create().Build();
        var bookedService = DentalServiceTestDataBuilder.Create().Build();
        var performedService = DentalServiceTestDataBuilder.Create()
            .WithName("Teeth Whitening")
            .WithCost(75m)
            .Build();

        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(bookedService.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.DentalServices.Add(bookedService);
            context.DentalServices.Add(performedService);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        var request = new CompleteAppointmentRequest
        {
            Treatments = [new CompleteAppointmentTreatmentRequest { ToothNumber = 11, ProcedureDetails = "Composite filling", Cost = 50m }],
            ServiceId = performedService.Id,
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dbAppointment = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id));
        Assert.Equal(AppointmentStatus.Completed, dbAppointment!.Status);
        Assert.Equal(performedService.Id, dbAppointment.ServiceId);

        // The service definitions themselves must remain untouched.
        var services = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.DentalServices
                .Where(s => s.Id == bookedService.Id || s.Id == performedService.Id)
                .ToListAsync());

        var dbBooked = services.Single(s => s.Id == bookedService.Id);
        Assert.Equal(bookedService.Name, dbBooked.Name);
        Assert.Equal(bookedService.Price, dbBooked.Price);

        var dbPerformed = services.Single(s => s.Id == performedService.Id);
        Assert.Equal("Teeth Whitening", dbPerformed.Name);
        Assert.Equal(75m, dbPerformed.Price);
    }

    [Fact]
    public async Task CompleteAppointment_WithUnknownService_ShouldReturnNotFoundAndNotComplete()
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

        var request = new CompleteAppointmentRequest { ServiceId = Guid.NewGuid() };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var dbAppointment = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id));
        Assert.Equal(AppointmentStatus.Scheduled, dbAppointment!.Status);
    }

    [Fact]
    public async Task CompleteAppointment_WithEmptyTreatments_ShouldCompleteWithoutRecords()
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

        var request = new CompleteAppointmentRequest { Treatments = [] };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var recordCount = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.TreatmentRecords.CountAsync(t => t.AppointmentId == appointment.Id));
        Assert.Equal(0, recordCount);
    }

    [Fact]
    public async Task CompleteAppointment_WithInvalidToothNumber_ShouldReturnBadRequest()
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

        var request = new CompleteAppointmentRequest
        {
            Treatments = [new CompleteAppointmentTreatmentRequest { ToothNumber = 99, ProcedureDetails = "Fill", Cost = 10m }],
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var dbAppointment = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id));
        Assert.Equal(AppointmentStatus.Scheduled, dbAppointment!.Status);
    }

    [Fact]
    public async Task CompleteAppointment_WithTreatments_ShouldNotCreateInvoiceOrPayment()
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

        var request = new CompleteAppointmentRequest
        {
            Treatments = [new CompleteAppointmentTreatmentRequest { ToothNumber = 11, ProcedureDetails = "Composite filling", Cost = 50m }],
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1.0/appointments/{appointment.Id}/complete",
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invoiceCount = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Invoices.CountAsync(i => i.AppointmentId == appointment.Id));
        var paymentCount = await webAppFactory.ExecuteDbContextAsync(async context =>
            await context.Payments.CountAsync());

        Assert.Equal(0, invoiceCount);
        Assert.Equal(0, paymentCount);
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

    // -------------------------------------------------------------------------
    // 7. PATCH /api/v1.0/appointments/{id}/no-show
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MarkNoShowAppointment_WithValidId_ShouldUpdateStatus()
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
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{appointment.Id}/no-show", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1.0/appointments/{appointment.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(result);
        Assert.Equal(appointment.Id, result!.Id);
        Assert.Equal(AppointmentStatus.NoShow, result.Status);
    }

    [Fact]
    public async Task MarkNoShowAppointment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await EnsureUserExistsAsync();
        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{nonExistentId}/no-show", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkNoShowAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var appointmentId = Guid.NewGuid();
        var response = await _client.PatchAsJsonAsync<object>($"/api/v1.0/appointments/{appointmentId}/no-show", null!, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

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
}