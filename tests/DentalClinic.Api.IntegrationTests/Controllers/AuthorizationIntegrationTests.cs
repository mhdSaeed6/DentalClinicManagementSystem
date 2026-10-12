using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Appointments;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.TreatmentRecords;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Builders;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

/// <summary>
/// End-to-end authorization tests: they authenticate with real JWTs issued by the
/// identity endpoint and hit the real controllers, so they exercise the actual
/// authentication + authorization pipeline (policies and handler ownership rules),
/// not a mocked user.
/// </summary>
[Collection(WebAppFactoryCollection.CollectionName)]
public class AuthorizationIntegrationTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    // ---------------------------------------------------------------------
    // Appointments – doctor isolation
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAppointments_AsDoctor_ReturnsOnlyOwnAppointments()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, doctorA) = await CreateDoctorUserAsync("authz.appt.doc.a@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.appt.doc.b@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointmentA = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctorA.Id).WithService(service.Id).Build();
        var appointmentB = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctorB.Id).WithService(service.Id).Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.AddRange(appointmentA, appointmentB);
            await context.SaveChangesAsync(default);
        });

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        // Act
        var response = await _client.GetAsync("/api/v1.0/appointments?pageNumber=1&pageSize=50");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AppointmentDto>>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items!, a => a.Id == appointmentA.Id);
        Assert.DoesNotContain(result.Items!, a => a.Id == appointmentB.Id);
        Assert.All(result.Items!, a => Assert.Equal(doctorA.Id, a.DoctorId));
    }

    [Fact]
    public async Task GetAppointmentById_AsDoctor_ForOwnAppointment_ReturnsOk()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorEmail, doctor) = await CreateDoctorUserAsync("authz.appt.doc.own@dentalclinic.com");
        var appointment = await SeedAppointmentAsync(doctor.Id);

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync($"/api/v1.0/appointments/{appointment.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointmentById_AsDoctor_ForAnotherDoctorsAppointment_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, _) = await CreateDoctorUserAsync("authz.appt.doc.c@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.appt.doc.d@dentalclinic.com");
        var appointmentB = await SeedAppointmentAsync(doctorB.Id);

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync($"/api/v1.0/appointments/{appointmentB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_AsDoctor_ForAnotherDoctor_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, _) = await CreateDoctorUserAsync("authz.appt.doc.e@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.appt.doc.f@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            await context.SaveChangesAsync(default);
        });

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        var request = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctorB.Id,
            ServiceId = service.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(2),
            DurationInMinutes = 30
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/appointments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompleteAppointment_AsReceptionist_IsAllowed()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (_, doctor) = await CreateDoctorUserAsync("authz.appt.doc.g@dentalclinic.com");
        var appointment = await SeedAppointmentAsync(doctor.Id);
        var receptionistEmail = await EnsureReceptionistAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        var response = await _client.PatchAsJsonAsync<object>(
            $"/api/v1.0/appointments/{appointment.Id}/complete", null!, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteAppointment_AsDoctor_ForAnotherDoctorsAppointment_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (_, doctorA) = await CreateDoctorUserAsync("authz.appt.doc.h@dentalclinic.com");
        var (doctorBEmail, _) = await CreateDoctorUserAsync("authz.appt.doc.i@dentalclinic.com");
        var appointmentA = await SeedAppointmentAsync(doctorA.Id);

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorBEmail, TestUsers.DoctorPassword));

        var response = await _client.PatchAsJsonAsync<object>(
            $"/api/v1.0/appointments/{appointmentA.Id}/complete", null!, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Treatment records – clinical write restriction
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateTreatmentRecord_AsReceptionist_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (_, doctor) = await CreateDoctorUserAsync("authz.treat.doc.a@dentalclinic.com");
        var patient = await SeedPatientAsync();
        var receptionistEmail = await EnsureReceptionistAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        var request = new CreateTreatmentRecordRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ToothNumber = 11,
            ProcedureDetails = "Receptionist should not be able to write this.",
            Cost = 100m
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTreatmentRecord_AsDoctor_ForAnotherDoctor_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, _) = await CreateDoctorUserAsync("authz.treat.doc.b@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.treat.doc.c@dentalclinic.com");
        var patient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        var request = new CreateTreatmentRecordRequest
        {
            PatientId = patient.Id,
            DoctorId = doctorB.Id,
            ToothNumber = 11,
            ProcedureDetails = "Attempt to attribute to another doctor.",
            Cost = 100m
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTreatmentRecord_AsDoctor_ForOwnVisit_ReturnsCreated()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorEmail, doctor) = await CreateDoctorUserAsync("authz.treat.doc.d@dentalclinic.com");
        var patient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword));

        var request = new CreateTreatmentRecordRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ToothNumber = 12,
            ProcedureDetails = "Composite filling on the upper left first premolar.",
            Cost = 80m
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Patient privacy – doctor isolation
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetPatientById_AsDoctor_ForUnrelatedPatient_ReturnsOk()
    {
        // Clinic staff (including doctors) share the full patient registry.
        await webAppFactory.ResetDatabaseAsync();

        var (doctorEmail, _) = await CreateDoctorUserAsync("authz.patient.doc.a@dentalclinic.com");
        var unrelatedPatient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync($"/api/v1.0/patients/{unrelatedPatient.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPatients_AsDoctor_ReturnsEveryClinicPatient()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorEmail, _) = await CreateDoctorUserAsync("authz.patient.doc.b@dentalclinic.com");
        var unrelatedPatient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync("/api/v1.0/patients?pageNumber=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<PatientDto>>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items!, p => p.Id == unrelatedPatient.Id);
    }

    [Fact]
    public async Task GetPatientById_AsAdmin_ForAnyPatient_ReturnsOk()
    {
        await webAppFactory.ResetDatabaseAsync();

        var patient = await SeedPatientAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));

        var response = await _client.GetAsync($"/api/v1.0/patients/{patient.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.NotNull(dto);
        Assert.Equal(patient.Id, dto!.Id);
    }

    // ---------------------------------------------------------------------
    // Invoices – doctor sees only invoices for their own appointments
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetInvoices_AsDoctor_ReturnsOnlyInvoicesLinkedToOwnAppointments()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, doctorA) = await CreateDoctorUserAsync("authz.inv.doc.a@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.inv.doc.b@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();

        var appointmentA = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctorA.Id).WithService(service.Id).Build();
        var appointmentB = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctorB.Id).WithService(service.Id).Build();

        Invoice invoiceA = null!;
        Invoice invoiceB = null!;
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.AddRange(appointmentA, appointmentB);
            invoiceA = Invoice.Create(patient.Id, 120m, appointmentA.Id).Value;
            invoiceB = Invoice.Create(patient.Id, 340m, appointmentB.Id).Value;
            context.Invoices.AddRange(invoiceA, invoiceB);
            await context.SaveChangesAsync(default);
        });

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync("/api/v1.0/invoices?pageNumber=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<InvoiceDto>>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items!, i => i.Id == invoiceA.Id);
        Assert.DoesNotContain(result.Items!, i => i.Id == invoiceB.Id);
    }

    [Fact]
    public async Task GetInvoiceById_AsDoctor_ForAnotherDoctorsInvoice_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (doctorAEmail, _) = await CreateDoctorUserAsync("authz.inv.doc.c@dentalclinic.com");
        var (_, doctorB) = await CreateDoctorUserAsync("authz.inv.doc.d@dentalclinic.com");

        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        var appointmentB = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctorB.Id).WithService(service.Id).Build();

        Invoice invoiceB = null!;
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointmentB);
            invoiceB = Invoice.Create(patient.Id, 340m, appointmentB.Id).Value;
            context.Invoices.Add(invoiceB);
            await context.SaveChangesAsync(default);
        });

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(doctorAEmail, TestUsers.DoctorPassword));

        var response = await _client.GetAsync($"/api/v1.0/invoices/{invoiceB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoiceById_AsReceptionist_ForAnyInvoice_ReturnsOk()
    {
        await webAppFactory.ResetDatabaseAsync();

        var (_, doctor) = await CreateDoctorUserAsync("authz.inv.doc.e@dentalclinic.com");
        var receptionistEmail = await EnsureReceptionistAsync();

        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id).WithDoctor(doctor.Id).WithService(service.Id).Build();

        Invoice invoice = null!;
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            invoice = Invoice.Create(patient.Id, 90m, appointment.Id).Value;
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(default);
        });

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        var response = await _client.GetAsync($"/api/v1.0/invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Administrative operations
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateDoctor_AsReceptionist_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var receptionistEmail = await EnsureReceptionistAsync();
        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        var request = new CreateDoctorRequest
        {
            FirstName = "Nadia",
            LastName = "Haddad",
            Specialization = "Orthodontics",
            Gender = "Female",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = "+963955555555",
                HasWhatsAppOnPrimary = true
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_AsReceptionist_ReturnsForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();

        var patient = await SeedPatientAsync();
        var receptionistEmail = await EnsureReceptionistAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(receptionistEmail, TestUsers.ReceptionistPassword));

        var response = await _client.DeleteAsync($"/api/v1.0/patients/{patient.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<(string Email, Doctor Doctor)> CreateDoctorUserAsync(string email)
    {
        AppUser user;

        using (var scope = webAppFactory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            user = (await userManager.FindByEmailAsync(email))!;

            if (user is null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

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
            doctor = DoctorTestDataBuilder.Create().WithName("Authz", "Doctor").Build();
            doctor.AssignUser(user.Id);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (email, doctor);
    }

    private async Task<string> EnsureReceptionistAsync()
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var email = TestUsers.Receptionist.Email!;
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = TestUsers.Receptionist;
            var created = await userManager.CreateAsync(user, TestUsers.ReceptionistPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, "Receptionist"))
        {
            await userManager.AddToRoleAsync(user, "Receptionist");
        }

        return email;
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var patient = PatientTestDataBuilder.Create().Build();
        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync(default);
        });

        return patient;
    }

    private async Task<Appointment> SeedAppointmentAsync(Guid doctorId)
    {
        var patient = PatientTestDataBuilder.Create().Build();
        var service = DentalServiceTestDataBuilder.Create().Build();
        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctorId)
            .WithService(service.Id)
            .Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(default);
        });

        return appointment;
    }
}
