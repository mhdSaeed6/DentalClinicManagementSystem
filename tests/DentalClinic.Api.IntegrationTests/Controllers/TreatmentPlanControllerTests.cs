using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.TreatmentPlans;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentPlans;
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
public sealed class TreatmentPlanControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    [Fact]
    public async Task TreatmentPlanEndpoints_ShouldSupportTheFullLifecycle()
    {
        await webAppFactory.ResetDatabaseAsync();
        await AuthenticateAsync(TestUsers.Admin.Email!, TestUsers.AdminPassword);

        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        // Create (draft).
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1.0/treatment-plans",
            new CreateTreatmentPlanRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Title = "Root canal plan",
                Notes = "Two clinical sessions"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var plan = await createResponse.Content.ReadFromJsonAsync<TreatmentPlanDto>();
        Assert.NotNull(plan);
        Assert.Equal(TreatmentPlanStatus.Draft, plan!.Status);

        // List with a patient filter.
        var listResponse = await _client.GetAsync($"/api/v1.0/treatment-plans?PatientId={patient.Id}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedList<TreatmentPlanDto>>();
        Assert.NotNull(list);
        Assert.Contains(list!.Items!, item => item.Id == plan.Id);

        // Update the header.
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}",
            new UpdateTreatmentPlanRequest
            {
                Title = "Root canal plan (revised)",
                Notes = "Updated notes"
            });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var detail = await GetPlanAsync(plan.Id);
        Assert.Equal("Root canal plan (revised)", detail.Title);
        Assert.Equal("Updated notes", detail.Notes);

        // Add three items across two sessions.
        var performedItem = await AddItemAsync(plan.Id, tooth: 26, cost: 300m, session: 1, "Root canal");
        await AddItemAsync(plan.Id, tooth: 36, cost: 100m, session: 2, "Filling");
        var removedItem = await AddItemAsync(plan.Id, tooth: 46, cost: 50m, session: 2, "Extraction");

        // Update an item.
        var updateItemResponse = await _client.PutAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{performedItem.Id}",
            new UpdateTreatmentPlanItemRequest
            {
                ToothNumber = 26,
                ProcedureDetails = "Root canal (revised)",
                EstimatedCost = 320m,
                SessionNumber = 1
            });
        Assert.Equal(HttpStatusCode.OK, updateItemResponse.StatusCode);
        var updatedItem = await updateItemResponse.Content.ReadFromJsonAsync<TreatmentPlanItemDto>();
        Assert.Equal(320m, updatedItem!.EstimatedCost);

        // Perform the first item with an actual cost.
        var performResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{performedItem.Id}/perform",
            new PerformTreatmentPlanItemRequest { ActualCost = 280m });
        Assert.Equal(HttpStatusCode.OK, performResponse.StatusCode);
        var performed = await performResponse.Content.ReadFromJsonAsync<TreatmentPlanItemDto>();
        Assert.Equal(TreatmentRecordStatus.Performed, performed!.Status);
        Assert.Equal(280m, performed.Cost);

        // Session totals: performed work in session 1, planned work in session 2.
        var sessionsResponse = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}/sessions");
        Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);
        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<TreatmentPlanSessionDto>>();
        Assert.NotNull(sessions);
        Assert.Equal(2, sessions!.Count);
        Assert.Equal(280m, sessions[0].PerformedTotal);
        Assert.Equal(150m, sessions[1].EstimatedTotal);

        // D-P4: a performed item can be reopened to Planned, then performed again.
        var reopenItemResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{performedItem.Id}/reopen"));
        Assert.Equal(HttpStatusCode.OK, reopenItemResponse.StatusCode);
        var reopened = await reopenItemResponse.Content.ReadFromJsonAsync<TreatmentPlanItemDto>();
        Assert.Equal(TreatmentRecordStatus.Planned, reopened!.Status);

        var reperformResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{performedItem.Id}/perform",
            new PerformTreatmentPlanItemRequest { ActualCost = 280m });
        Assert.Equal(HttpStatusCode.OK, reperformResponse.StatusCode);

        // A cancelled item stays cancelled (reopen is performed-only) and can be
        // removed from the plan as a soft delete.
        var cancelItemResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{removedItem.Id}/cancel"));
        Assert.Equal(HttpStatusCode.OK, cancelItemResponse.StatusCode);
        var cancelled = await cancelItemResponse.Content.ReadFromJsonAsync<TreatmentPlanItemDto>();
        Assert.Equal(TreatmentRecordStatus.Cancelled, cancelled!.Status);

        var removeItemResponse = await _client.DeleteAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{removedItem.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeItemResponse.StatusCode);

        // Draft → Active → Completed.
        var activateResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/treatment-plans/{plan.Id}/activate"));
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var completeResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/treatment-plans/{plan.Id}/complete"));
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        detail = await GetPlanAsync(plan.Id);
        Assert.Equal(TreatmentPlanStatus.Completed, detail.Status);
        Assert.Equal(2, detail.ItemCount);
        Assert.Equal(280m, detail.PerformedTotal);
        Assert.Equal(100m, detail.EstimatedTotal);

        // Deleting a plan with performed work requires the explicit flag.
        var unconfirmedDelete = await _client.DeleteAsync($"/api/v1.0/treatment-plans/{plan.Id}");
        Assert.Equal(HttpStatusCode.Conflict, unconfirmedDelete.StatusCode);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}?confirmPerformedItems=true");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task GetTreatmentPlan_AsOwner_ShouldSucceed_AndAsAnotherDoctor_ShouldReturnForbidden()
    {
        await webAppFactory.ResetDatabaseAsync();
        var (ownerEmail, owner) = await CreateDoctorUserAsync("plan.owner@dentalclinic.com");
        var (otherEmail, _) = await CreateDoctorUserAsync("plan.other@dentalclinic.com");
        var patient = await SeedPatientAsync();
        var plan = TreatmentPlan.Create(patient.Id, owner.Id, "Private plan").Value;

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.TreatmentPlans.Add(plan);
            await context.SaveChangesAsync(default);
        });

        // Positive control: the owning doctor's token carries the DoctorId claim
        // and the handler scope lets them read their own plan.
        await AuthenticateAsync(ownerEmail, TestUsers.DoctorPassword);
        var ownerResponse = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);

        var ownerSessions = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}/sessions");
        Assert.Equal(HttpStatusCode.OK, ownerSessions.StatusCode);

        await AuthenticateAsync(otherEmail, TestUsers.DoctorPassword);
        var otherResponse = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);

        var otherSessions = await _client.GetAsync($"/api/v1.0/treatment-plans/{plan.Id}/sessions");
        Assert.Equal(HttpStatusCode.Forbidden, otherSessions.StatusCode);
    }

    [Fact]
    public async Task PerformTreatmentPlanItem_ShouldAppearInGeneratedInvoice()
    {
        await webAppFactory.ResetDatabaseAsync();
        await AuthenticateAsync(TestUsers.Admin.Email!, TestUsers.AdminPassword);

        var (patient, doctor) = await SeedPatientAndDoctorAsync();
        var service = DentalService.Create("Consultation", "Consultation", 50m).Value;
        var appointment = AppointmentTestDataBuilder.Create()
            .WithPatient(patient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Completed()
            .Build();
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Billable plan").Value;
        var item = TreatmentRecord.CreatePlanItem(
            plan.Id,
            patient.Id,
            doctor.Id,
            36,
            "Filling",
            100m).Value;

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.DentalServices.Add(service);
            context.Appointments.Add(appointment);
            context.TreatmentPlans.Add(plan);
            context.TreatmentRecords.Add(item);
            await context.SaveChangesAsync(default);
        });

        var performResponse = await _client.PostAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{item.Id}/perform",
            new PerformTreatmentPlanItemRequest
            {
                ActualCost = 80m,
                AppointmentId = appointment.Id
            });
        Assert.Equal(HttpStatusCode.OK, performResponse.StatusCode);

        var invoiceResponse = await _client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1.0/invoices/from-appointment/{appointment.Id}"));
        Assert.Equal(HttpStatusCode.OK, invoiceResponse.StatusCode);

        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<GenerateAppointmentInvoiceResult>();
        Assert.NotNull(invoice);
        Assert.Equal(80m, invoice!.TreatmentSubtotal);
    }

    [Fact]
    public async Task PerformTreatmentPlanItem_WithAnotherPatientsAppointment_ShouldReturnBadRequest()
    {
        await webAppFactory.ResetDatabaseAsync();
        await AuthenticateAsync(TestUsers.Admin.Email!, TestUsers.AdminPassword);

        var (patient, doctor) = await SeedPatientAndDoctorAsync();
        var otherPatient = await SeedPatientAsync();
        var service = DentalService.Create("Consultation", "Consultation", 50m).Value;
        var otherAppointment = AppointmentTestDataBuilder.Create()
            .WithPatient(otherPatient.Id)
            .WithDoctor(doctor.Id)
            .WithService(service.Id)
            .Build();

        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Scoped plan").Value;
        var item = TreatmentRecord.CreatePlanItem(
            plan.Id,
            patient.Id,
            doctor.Id,
            36,
            "Filling",
            100m).Value;

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.DentalServices.Add(service);
            context.Appointments.Add(otherAppointment);
            context.TreatmentPlans.Add(plan);
            context.TreatmentRecords.Add(item);
            await context.SaveChangesAsync(default);
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/v1.0/treatment-plans/{plan.Id}/items/{item.Id}/perform",
            new PerformTreatmentPlanItemRequest
            {
                ActualCost = 80m,
                AppointmentId = otherAppointment.Id
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<TreatmentPlanDetailDto> GetPlanAsync(Guid planId)
    {
        var response = await _client.GetAsync($"/api/v1.0/treatment-plans/{planId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<TreatmentPlanDetailDto>();
        Assert.NotNull(detail);
        return detail!;
    }

    private async Task<TreatmentPlanItemDto> AddItemAsync(
        Guid planId,
        int tooth,
        decimal cost,
        int session,
        string procedure)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1.0/treatment-plans/{planId}/items",
            new AddTreatmentPlanItemRequest
            {
                ToothNumber = tooth,
                ProcedureDetails = procedure,
                EstimatedCost = cost,
                SessionNumber = session
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<TreatmentPlanItemDto>();
        Assert.NotNull(item);
        return item!;
    }

    private async Task AuthenticateAsync(string email, string password)
    {
        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(email, password));
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientAndDoctorAsync()
    {
        var patient = await SeedPatientAsync();
        var doctor = DoctorTestDataBuilder.Create().WithName("Plan", "Doctor").Build();

        await webAppFactory.ExecuteDbContextAsync(async context =>
        {
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync(default);
        });

        return (patient, doctor);
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

    private async Task<(string Email, Doctor Doctor)> CreateDoctorUserAsync(string email)
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var existingUser = await userManager.FindByEmailAsync(email);
        var user = existingUser ?? new AppUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        if (existingUser is null)
        {
            var created = await userManager.CreateAsync(user, TestUsers.DoctorPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, "Doctor"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Doctor");
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // Identity users survive Respawn while doctor rows do not, so reuse the
        // profile when it already exists instead of creating a second Doctor
        // row (which would make the DoctorId claim ambiguous).
        var doctor = await webAppFactory.ExecuteDbContextAsync(context =>
            context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id));

        if (doctor is null)
        {
            doctor = DoctorTestDataBuilder.Create().WithName("Plan", "Doctor").Build();
            doctor.AssignUser(user.Id);
            await webAppFactory.ExecuteDbContextAsync(async context =>
            {
                context.Doctors.Add(doctor);
                await context.SaveChangesAsync(default);
            });
        }

        return (email, doctor);
    }
}