using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ClosedXML.Excel;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Domain.Imports;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class ImportControllerTests : IAsyncLifetime
{
    private const string WorkbookContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] s_defaultHeaders =
    [
        "First name",
        "Last name",
        "Primary phone",
        "Date of birth",
        "Gender",
        "Diabetes",
        "Allergies",
    ];

    private static int s_phoneSeed;

    private readonly WebAppFactory _factory;
    private readonly AppHttpClient _client;

    public ImportControllerTests(WebAppFactory factory)
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
    public async Task Preview_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        _client.ClearAuthorizationHeader();

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook([ValidRow()]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Preview_AsReceptionist_ShouldReturnForbidden()
    {
        await EnsureReceptionistExistsAsync();

        _client.SetAuthorizationHeader(
            await _client.GenerateTokenAsync(TestUsers.Receptionist.Email!, TestUsers.ReceptionistPassword));

        try
        {
            var response = await PostWorkbookAsync(
                "/api/v1.0/imports/patients/preview",
                BuildWorkbook([ValidRow()]));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
        }
    }

    [Fact]
    public async Task Commit_AsReceptionist_ShouldReturnForbidden_AndWriteNothing()
    {
        await EnsureReceptionistExistsAsync();

        _client.SetAuthorizationHeader(
            await _client.GenerateTokenAsync(TestUsers.Receptionist.Email!, TestUsers.ReceptionistPassword));

        try
        {
            var response = await PostWorkbookAsync(
                "/api/v1.0/imports/patients",
                BuildWorkbook([ValidRow(lastName: "ImportReceptionist")]));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.SetAuthorizationHeader(await _client.GenerateTokenAsync(TestUsers.Admin));
        }

        var patients = await SearchPatientsAsync("ImportReceptionist");
        Assert.Equal(0, patients.TotalCount);
    }

    // -------------------------------------------------------------------------
    // Preview
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Preview_WithValidWorkbook_ShouldValidateWithoutWriting()
    {
        var workbook = BuildWorkbook(
        [
            ValidRow(lastName: "ImportPreview"),
            ValidRow(lastName: "ImportPreview"),
        ]);

        var response = await PostWorkbookAsync("/api/v1.0/imports/patients/preview", workbook);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(2, preview.TotalRows);
        Assert.Equal(2, preview.ValidRows);
        Assert.Equal(0, preview.InvalidRows);
        Assert.Equal(2, preview.Rows.Count);
        Assert.All(preview.Rows, row => Assert.True(row.IsValid));

        // A dry run must not insert anything.
        var patients = await SearchPatientsAsync("ImportPreview");
        Assert.Equal(0, patients.TotalCount);
    }

    [Fact]
    public async Task Preview_WithMissingRequiredColumn_ShouldReturnBadRequest()
    {
        var headersWithoutGender = s_defaultHeaders.Where(header => header != "Gender").ToArray();

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook([ValidRow()], headersWithoutGender));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preview_WithMalformedWorkbook_ShouldReturnBadRequest()
    {
        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            "this is not a workbook"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preview_WithBlankPhone_ShouldReportRowError()
    {
        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook([ValidRow(phone: string.Empty)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(1, preview.InvalidRows);

        var row = Assert.Single(preview.Rows);
        Assert.False(row.IsValid);
        Assert.Contains(row.Errors, error => error.Column == "PrimaryPhone");
    }

    [Fact]
    public async Task Preview_WithInvalidGender_ShouldReportRowError()
    {
        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook([ValidRow(gender: "Unknown")]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(1, preview.InvalidRows);
        Assert.Contains(
            Assert.Single(preview.Rows).Errors,
            error => error.Column == "Gender");
    }

    [Fact]
    public async Task Preview_WithDuplicatePhoneInWorkbook_ShouldRejectTheLaterRow()
    {
        var phone = NextPhone();

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook(
            [
                ValidRow(phone: phone),
                ValidRow(phone: phone),
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(1, preview.ValidRows);
        Assert.Equal(1, preview.InvalidRows);

        Assert.True(preview.Rows[0].IsValid);
        Assert.Contains(
            preview.Rows[1].Errors,
            error => error.Message.Contains("Duplicate phone number", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preview_WithExistingPatientPhone_ShouldRejectRow()
    {
        var phone = NextPhone();
        await CreateTestPatientAsync("Existing", phone);

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            BuildWorkbook([ValidRow(phone: phone)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(1, preview.InvalidRows);
        Assert.Contains(
            Assert.Single(preview.Rows).Errors,
            error => error.Message.Contains("already exists", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preview_WithExplicitMapping_ShouldUseSuppliedHeaders()
    {
        string[] headers = ["A", "B", "C", "D", "E"];
        var workbook = BuildWorkbook(
        [
            [ "Mapped", "Importer", NextPhone(), "1991-02-03", "Male" ],
        ],
        headers);

        var mapping = """{"FirstName":"A","LastName":"B","PrimaryPhone":"C","DateOfBirth":"D","Gender":"E"}""";

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients/preview",
            workbook,
            mapping: mapping);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<PatientImportPreviewDto>())!;
        Assert.Equal(1, preview.ValidRows);
        Assert.Equal("Mapped Importer", Assert.Single(preview.Rows).DisplayName);
    }

    // -------------------------------------------------------------------------
    // Commit
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Commit_WithValidWorkbook_ShouldInsertPatientsAndCompleteJob()
    {
        var firstPhone = NextPhone();
        var secondPhone = NextPhone();

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients",
            BuildWorkbook(
            [
                ValidRow(firstName: "Sara", lastName: "ImportHappy", phone: firstPhone, diabetes: "Yes", allergies: "Penicillin"),
                ValidRow(firstName: "Omar", lastName: "ImportHappy", phone: secondPhone, gender: "Male"),
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = (await response.Content.ReadFromJsonAsync<PatientImportReportDto>())!;
        Assert.Equal(ImportJobStatus.Completed, report.Status);
        Assert.Equal(2, report.TotalRows);
        Assert.Equal(2, report.ImportedRows);
        Assert.Equal(0, report.RejectedRows);
        Assert.NotEqual(Guid.Empty, report.JobId);
        Assert.Empty(report.Errors);

        var patients = await SearchPatientsAsync("ImportHappy");
        Assert.Equal(2, patients.TotalCount);

        var sara = patients.Items!.Single(patient => patient.FirstName == "Sara");
        Assert.Equal(firstPhone, sara.PrimaryPhone);
        Assert.Equal(new DateTime(1990, 12, 31), sara.DateOfBirth.Date);
        Assert.Equal(1, sara.Gender);
        Assert.True(sara.MedicalHistory.HasDiabetes);
        Assert.Equal("Penicillin", sara.MedicalHistory.Allergies);

        // The job is retrievable after the fact.
        var jobResponse = await _client.GetAsync($"/api/v1.0/imports/{report.JobId}");
        Assert.Equal(HttpStatusCode.OK, jobResponse.StatusCode);

        var job = (await jobResponse.Content.ReadFromJsonAsync<ImportJobDto>())!;
        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Equal(2, job.ImportedRows);
    }

    [Fact]
    public async Task Commit_WithRejectedRow_ShouldPersistFailedJobAndImportNothing()
    {
        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients",
            BuildWorkbook(
            [
                ValidRow(firstName: "Rejected", lastName: "ImportMixed"),
                ValidRow(firstName: "Broken", lastName: "ImportMixed", gender: "Unknown"),
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = (await response.Content.ReadFromJsonAsync<PatientImportReportDto>())!;
        Assert.Equal(ImportJobStatus.Failed, report.Status);
        Assert.Equal(2, report.TotalRows);
        Assert.Equal(0, report.ImportedRows);
        Assert.Equal(1, report.RejectedRows);
        Assert.NotEmpty(report.Errors);

        // All-or-nothing: the otherwise valid first row was not written.
        var patients = await SearchPatientsAsync("ImportMixed");
        Assert.Equal(0, patients.TotalCount);

        // The rejection report downloads as CSV.
        var csvResponse = await _client.GetAsync($"/api/v1.0/imports/{report.JobId}/errors");
        Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
        Assert.Equal("text/csv", csvResponse.Content.Headers.ContentType?.MediaType);

        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains("Row,Column,Message,Value", csv, StringComparison.Ordinal);
        Assert.Contains("Gender", csv, StringComparison.Ordinal);
        Assert.Contains("3", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Commit_WithDuplicatePhoneInDatabase_ShouldReturnFailedReport()
    {
        var phone = NextPhone();
        await CreateTestPatientAsync("Already", phone);

        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients",
            BuildWorkbook([ValidRow(lastName: "ImportDuplicate", phone: phone)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = (await response.Content.ReadFromJsonAsync<PatientImportReportDto>())!;
        Assert.Equal(ImportJobStatus.Failed, report.Status);
        Assert.Equal(0, report.ImportedRows);
        Assert.Contains(
            report.Errors,
            error => error.Message.Contains("already exists", StringComparison.Ordinal));

        var patients = await SearchPatientsAsync("ImportDuplicate");
        Assert.Equal(0, patients.TotalCount);
    }

    [Fact]
    public async Task Commit_WithEmptyWorkbook_ShouldReturnBadRequest()
    {
        var response = await PostWorkbookAsync(
            "/api/v1.0/imports/patients",
            BuildWorkbook([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Job + template reads
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetImportJob_ForUnknownId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1.0/imports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetImportJobErrors_ForUnknownId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1.0/imports/{Guid.NewGuid()}/errors");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTemplate_ShouldReturnWorkbookWithExpectedHeaders()
    {
        var response = await _client.GetAsync("/api/v1.0/imports/patients/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkbookContentType, response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 2);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("First name", sheet.Cell(1, 1).GetString());
        Assert.Equal("Last name", sheet.Cell(1, 2).GetString());
        Assert.Equal("Primary phone", sheet.Cell(1, 3).GetString());
        Assert.Equal("Date of birth", sheet.Cell(1, 4).GetString());
        Assert.Equal("Gender", sheet.Cell(1, 5).GetString());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string NextPhone() =>
        $"+1555{Interlocked.Increment(ref s_phoneSeed):D7}";

    private static string?[] ValidRow(
        string firstName = "Import",
        string lastName = "Patient",
        string? phone = null,
        string? gender = "Female",
        string? diabetes = "No",
        string? allergies = null) =>
        [firstName, lastName, phone ?? NextPhone(), "1990-12-31", gender, diabetes, allergies];

    private static byte[] BuildWorkbook(
        IEnumerable<string?[]> rows,
        string[]? headers = null)
    {
        var columns = headers ?? s_defaultHeaders;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Patients");

        for (var index = 0; index < columns.Length; index++)
        {
            sheet.Cell(1, index + 1).Value = columns[index];
        }

        var rowNumber = 2;

        foreach (var row in rows)
        {
            for (var index = 0; index < columns.Length; index++)
            {
                if (index < row.Length && row[index] is { } value)
                {
                    sheet.Cell(rowNumber, index + 1).Value = value;
                }
            }

            rowNumber++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private async Task<HttpResponseMessage> PostWorkbookAsync(
        string url,
        byte[] workbook,
        string fileName = "patients.xlsx",
        string? mapping = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(workbook);
        file.Headers.ContentType = new MediaTypeHeaderValue(WorkbookContentType);
        form.Add(file, "file", fileName);

        if (!string.IsNullOrWhiteSpace(mapping))
        {
            form.Add(new StringContent(mapping, System.Text.Encoding.UTF8, "application/json"), "mapping");
        }

        return await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, url) { Content = form });
    }

    private async Task<PaginatedList<PatientDto>> SearchPatientsAsync(string lastName)
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/patients?PageNumber=1&PageSize=100&SearchTerm={Uri.EscapeDataString(lastName)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<PaginatedList<PatientDto>>())!;
    }

    private async Task<PatientDto> CreateTestPatientAsync(string firstName, string phone)
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/patients", new CreatePatientRequest
        {
            FirstName = firstName,
            LastName = $"Import{Guid.NewGuid():N}"[..14],
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

        return (await response.Content.ReadFromJsonAsync<PatientDto>())!;
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