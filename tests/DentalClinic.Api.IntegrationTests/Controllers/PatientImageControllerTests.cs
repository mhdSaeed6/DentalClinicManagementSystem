using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Contracts.Requests.TreatmentRecords;
using DentalClinic.Domain.Patients;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class PatientImageControllerTests : IAsyncLifetime
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    private static readonly byte[] JpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01
    ];

    private static int s_phoneSeed;

    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public PatientImageControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Authorization
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPatientImages_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _client.ClearAuthorizationHeader();

        var response = await _client.GetAsync($"/api/v1.0/patients/{Guid.NewGuid()}/images");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_AsReceptionist_ShouldReturnForbidden()
    {
        // Arrange — receptionists may read images but never author clinical data.
        await EnsureReceptionistExistsAsync();

        var patientId = await CreateTestPatientAsync();

        _client.SetAuthorizationHeader(
            await _client.GenerateTokenAsync(TestUsers.Receptionist.Email!, TestUsers.ReceptionistPassword));

        try
        {
            // Act
            using var form = BuildUpload(PngBytes, "xray.png", "image/png", PatientImageKind.Xray);

            var response = await PostMultipartAsync($"/api/v1.0/patients/{patientId}/images", form);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
        }
    }

    // -------------------------------------------------------------------------
    // Upload + read-back
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UploadImage_WithValidPng_ShouldReturnCreatedAndServeContent()
    {
        var patientId = await CreateTestPatientAsync();

        // Act
        var response = await UploadAsync(
            patientId,
            PngBytes,
            "xray.png",
            "image/png",
            PatientImageKind.Xray,
            toothNumber: 16);

        // Assert — created row carries the server-detected format.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.Content.ReadFromJsonAsync<PatientImageDto>())!;
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(patientId, created.PatientId);
        Assert.Equal(PatientImageKind.Xray, created.Kind);
        Assert.Equal("xray.png", created.FileName);
        Assert.Equal("image/png", created.ContentType);
        Assert.Equal(PngBytes.Length, created.SizeBytes);
        Assert.Equal(16, created.ToothNumber);
        Assert.False(string.IsNullOrWhiteSpace(created.UploadedBy));

        // The list returns the metadata.
        var list = await _client.GetAsync($"/api/v1.0/patients/{patientId}/images");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var images = (await list.Content.ReadFromJsonAsync<List<PatientImageDto>>())!;
        Assert.Contains(images, i => i.Id == created.Id);

        // The content endpoint streams the exact bytes back as the stored type.
        var content = await _client.GetAsync($"/api/v1.0/patients/{patientId}/images/{created.Id}/content");
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("image/png", content.Content.Headers.ContentType?.MediaType);

        var bytes = await content.Content.ReadAsByteArrayAsync();
        Assert.Equal(PngBytes, bytes);
    }

    [Fact]
    public async Task UploadImage_WithJpeg_ShouldAcceptAndStoreJpegType()
    {
        var patientId = await CreateTestPatientAsync();

        var response = await UploadAsync(patientId, JpegBytes, "smile.jpg", "image/jpeg", PatientImageKind.Before);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.Content.ReadFromJsonAsync<PatientImageDto>())!;
        Assert.Equal("image/jpeg", created.ContentType);
        Assert.Equal(PatientImageKind.Before, created.Kind);
    }

    [Fact]
    public async Task UploadImage_WhenDeclaredTypeDoesNotMatchBytes_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();

        // Act — JPEG bytes declared as a PNG.
        var response = await UploadAsync(patientId, JpegBytes, "fake.png", "image/png", PatientImageKind.Xray);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithUnsupportedContent_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();

        var notAnImage = "#!/bin/sh\necho not-an-image\n"u8.ToArray();

        var response = await UploadAsync(
            patientId,
            notAnImage,
            "script.png",
            "image/png",
            PatientImageKind.Other);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WhenFileExceedsLimit_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();

        var oversized = new byte[(10 * 1024 * 1024) + 1];
        PngBytes.CopyTo(oversized, 0);

        var response = await UploadAsync(patientId, oversized, "huge.png", "image/png", PatientImageKind.Xray);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_ForUnknownPatient_ShouldReturnNotFound()
    {
        var response = await UploadAsync(
            Guid.NewGuid(),
            PngBytes,
            "xray.png",
            "image/png",
            PatientImageKind.Xray);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithUnknownKind_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();

        var response = await UploadAsync(patientId, PngBytes, "xray.png", "image/png", (PatientImageKind)99);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithoutFile_ShouldReturnBadRequest()
    {
        var patientId = await CreateTestPatientAsync();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(((int)PatientImageKind.Xray).ToString(CultureInfo.InvariantCulture)), "kind");

        var response = await PostMultipartAsync($"/api/v1.0/patients/{patientId}/images", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Treatment links
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UploadImage_WithTreatmentFromAnotherPatient_ShouldReturnBadRequest()
    {
        var doctorId = await CreateTestDoctorAsync();
        var ownerPatientId = await CreateTestPatientAsync();
        var otherPatientId = await CreateTestPatientAsync();

        var treatmentId = await CreateTestTreatmentAsync(ownerPatientId, doctorId, toothNumber: 16);

        // Act — the treatment belongs to the other patient.
        var response = await UploadAsync(
            otherPatientId,
            PngBytes,
            "xray.png",
            "image/png",
            PatientImageKind.Xray,
            treatmentRecordId: treatmentId,
            toothNumber: 16);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithMismatchedTooth_ShouldReturnBadRequest()
    {
        var doctorId = await CreateTestDoctorAsync();
        var patientId = await CreateTestPatientAsync();

        var treatmentId = await CreateTestTreatmentAsync(patientId, doctorId, toothNumber: 16);

        // Act — the treatment is recorded on tooth 16, the upload claims 11.
        var response = await UploadAsync(
            patientId,
            PngBytes,
            "xray.png",
            "image/png",
            PatientImageKind.Xray,
            treatmentRecordId: treatmentId,
            toothNumber: 11);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithMatchingTreatmentAndTooth_ShouldReturnCreated()
    {
        var doctorId = await CreateTestDoctorAsync();
        var patientId = await CreateTestPatientAsync();

        var treatmentId = await CreateTestTreatmentAsync(patientId, doctorId, toothNumber: 16);

        var response = await UploadAsync(
            patientId,
            PngBytes,
            "xray.png",
            "image/png",
            PatientImageKind.Xray,
            treatmentRecordId: treatmentId,
            toothNumber: 16);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.Content.ReadFromJsonAsync<PatientImageDto>())!;
        Assert.Equal(treatmentId, created.TreatmentRecordId);
        Assert.Equal(16, created.ToothNumber);
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteImage_ShouldReturnNoContent_AndHideIt()
    {
        var patientId = await CreateTestPatientAsync();
        var created = await UploadAndReadAsync(patientId, PngBytes, "xray.png", "image/png", PatientImageKind.Xray);

        var delete = await _client.DeleteAsync($"/api/v1.0/patients/{patientId}/images/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // The list no longer reports it and the content endpoint treats it as gone.
        var list = await _client.GetAsync($"/api/v1.0/patients/{patientId}/images");
        var images = (await list.Content.ReadFromJsonAsync<List<PatientImageDto>>())!;
        Assert.DoesNotContain(images, i => i.Id == created.Id);

        var content = await _client.GetAsync($"/api/v1.0/patients/{patientId}/images/{created.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, content.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<HttpResponseMessage> UploadAsync(
        Guid patientId,
        byte[] content,
        string fileName,
        string contentType,
        PatientImageKind kind,
        Guid? treatmentRecordId = null,
        int? toothNumber = null)
    {
        using var form = BuildUpload(content, fileName, contentType, kind, treatmentRecordId, toothNumber);

        return await PostMultipartAsync($"/api/v1.0/patients/{patientId}/images", form);
    }

    private Task<HttpResponseMessage> PostMultipartAsync(string url, HttpContent content) =>
        _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = content });

    private async Task<PatientImageDto> UploadAndReadAsync(
        Guid patientId,
        byte[] content,
        string fileName,
        string contentType,
        PatientImageKind kind)
    {
        var response = await UploadAsync(patientId, content, fileName, contentType, kind);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<PatientImageDto>())!;
    }

    private static MultipartFormDataContent BuildUpload(
        byte[] content,
        string fileName,
        string contentType,
        PatientImageKind kind,
        Guid? treatmentRecordId = null,
        int? toothNumber = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(((int)kind).ToString(CultureInfo.InvariantCulture)), "kind");

        if (treatmentRecordId.HasValue)
        {
            form.Add(new StringContent(treatmentRecordId.Value.ToString()), "treatmentRecordId");
        }

        if (toothNumber.HasValue)
        {
            form.Add(new StringContent(toothNumber.Value.ToString(CultureInfo.InvariantCulture)), "toothNumber");
        }

        return form;
    }

    private async Task<Guid> CreateTestPatientAsync()
    {
        var phone = $"+1555{Interlocked.Increment(ref s_phoneSeed):D7}";

        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", new CreatePatientRequest
        {
            FirstName = "Image",
            LastName = $"Patient{Guid.NewGuid():N}"[..14],
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = phone
            },
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            MedicalHistory = new MedicalHistoryRequest
            {
                HasDiabetes = false,
                HasHeartDisease = false
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var patient = (await response.Content.ReadFromJsonAsync<PatientDto>())!;
        return patient.Id;
    }

    private async Task<Guid> CreateTestDoctorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/doctors", new CreateDoctorRequest
        {
            FirstName = "Image",
            LastName = $"Doctor{Guid.NewGuid():N}"[..12],
            Specialization = "Radiology",
            Gender = "Male",
            ContactInfo = new CreateContactInfoRequest
            {
                PrimaryPhone = $"+1555{Interlocked.Increment(ref s_phoneSeed):D7}"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var doctor = (await response.Content.ReadFromJsonAsync<DoctorDto>())!;
        return doctor.Id;
    }

    private async Task<Guid> CreateTestTreatmentAsync(Guid patientId, Guid doctorId, int toothNumber)
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/treatment-records", new CreateTreatmentRecordRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ToothNumber = toothNumber,
            ProcedureDetails = "Periapical radiograph",
            Cost = 0m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var treatment = (await response.Content.ReadFromJsonAsync<TreatmentRecordDto>())!;
        return treatment.Id;
    }

    private async Task EnsureReceptionistExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var receptionist = await userManager.FindByIdAsync(TestUsers.Receptionist.Id);

        if (receptionist is null)
        {
            receptionist = TestUsers.Receptionist;
            await userManager.CreateAsync(receptionist, TestUsers.ReceptionistPassword);
        }

        if (!await userManager.IsInRoleAsync(receptionist, "Receptionist"))
        {
            await userManager.AddToRoleAsync(receptionist, "Receptionist");
        }
    }
}