using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DentalClinic.Web.Components;

namespace DentalClinic.Web.Api;

/// <summary>
/// Thin, strongly typed HTTP client over the DentalClinic REST API (v1).
/// Handles bearer token injection and transparent refresh-token rotation.
/// </summary>
public sealed class ApiClient
{
    private const string Prefix = "api/v1/";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ClinicSessionState _session;

    public ApiClient(HttpClient http, ClinicSessionState session)
    {
        _http = http;
        _session = session;
    }

    // -----------------------------------------------------------------------
    // Authentication
    // -----------------------------------------------------------------------

    /// <summary>Authenticates the user and stores the resulting session tokens.</summary>
    public async Task<AppUserDto> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var tokens = await PostAsync<TokenResponse>(
            "identity/token/generate",
            new LoginRequest { Email = email, Password = password },
            ct);

        _session.SetTokens(tokens);

        var user = await GetAsync<AppUserDto>("identity/current-user/claims", ct);
        _session.SetUser(user);
        return user;
    }

    public Task<AppUserDto> GetCurrentUserAsync(CancellationToken ct = default) =>
        GetAsync<AppUserDto>("identity/current-user/claims", ct);

    // -----------------------------------------------------------------------
    // Patients
    // -----------------------------------------------------------------------

    public Task<PaginatedList<PatientDto>> GetPatientsAsync(int page, int pageSize, string? search, CancellationToken ct = default) =>
        GetAsync<PaginatedList<PatientDto>>("patients?" + Query(("PageNumber", page), ("PageSize", pageSize), ("SearchTerm", search)), ct);

    public Task<PatientDto> GetPatientAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PatientDto>($"patients/{id}", ct);

    public Task<PatientDto> CreatePatientAsync(PatientFormRequest request, CancellationToken ct = default) =>
        PostAsync<PatientDto>("patients", request, ct);

    public Task<PatientDto> UpdatePatientAsync(Guid id, PatientFormRequest request, CancellationToken ct = default) =>
        PutAsync<PatientDto>($"patients/{id}", request, ct);

    public Task DeletePatientAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync($"patients/{id}", ct);

    // -----------------------------------------------------------------------
    // Doctors
    // -----------------------------------------------------------------------

    public Task<PaginatedList<DoctorDto>> GetDoctorsAsync(int page, int pageSize, string? search, CancellationToken ct = default) =>
        GetAsync<PaginatedList<DoctorDto>>("doctors?" + Query(("PageNumber", page), ("PageSize", pageSize), ("SearchTerm", search)), ct);

    public Task<DoctorDto> GetDoctorAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<DoctorDto>($"doctors/{id}", ct);

    public Task<DoctorDto> CreateDoctorAsync(DoctorFormRequest request, CancellationToken ct = default) =>
        PostAsync<DoctorDto>("doctors", request, ct);

    public Task<DoctorDto> UpdateDoctorAsync(Guid id, DoctorFormRequest request, CancellationToken ct = default) =>
        PutAsync<DoctorDto>($"doctors/{id}", request, ct);

    public Task DeleteDoctorAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync($"doctors/{id}", ct);

    // -----------------------------------------------------------------------
    // Dental services
    // -----------------------------------------------------------------------

    public Task<PaginatedList<DentalServiceDto>> GetServicesAsync(int page, int pageSize, string? search, CancellationToken ct = default) =>
        GetAsync<PaginatedList<DentalServiceDto>>("dentalservice?" + Query(("PageNumber", page), ("PageSize", pageSize), ("SearchTerm", search)), ct);

    public Task<DentalServiceDto> GetServiceAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<DentalServiceDto>($"dentalservice/{id}", ct);

    public Task<DentalServiceDto> CreateServiceAsync(DentalServiceFormRequest request, CancellationToken ct = default) =>
        PostAsync<DentalServiceDto>("dentalservice", request, ct);

    public Task<DentalServiceDto> UpdateServiceAsync(Guid id, DentalServiceFormRequest request, CancellationToken ct = default) =>
        PutAsync<DentalServiceDto>($"dentalservice/{id}", request, ct);

    public Task DeleteServiceAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync($"dentalservice/{id}", ct);

    // -----------------------------------------------------------------------
    // Appointments
    // -----------------------------------------------------------------------

    public Task<PaginatedList<AppointmentDto>> GetAppointmentsAsync(
        int page,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? serviceId = null,
        CancellationToken ct = default)
    {
        var query = Query(
            ("PageNumber", page),
            ("PageSize", pageSize),
            ("PatientId", patientId),
            ("DoctorId", doctorId),
            ("ServiceId", serviceId));

        return GetAsync<PaginatedList<AppointmentDto>>($"appointments?{query}", ct);
    }

    public Task<AppointmentDto> GetAppointmentAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<AppointmentDto>($"appointments/{id}", ct);

    public Task<AppointmentDto> CreateAppointmentAsync(AppointmentFormRequest request, CancellationToken ct = default) =>
        PostAsync<AppointmentDto>("appointments", request, ct);

    public Task<AppointmentDto> RescheduleAppointmentAsync(Guid id, RescheduleAppointmentRequest request, CancellationToken ct = default) =>
        PutAsync<AppointmentDto>($"appointments/{id}/reschedule", request, ct);

    public Task<AppointmentDto> CompleteAppointmentAsync(Guid id, CompleteAppointmentRequest? request = null, CancellationToken ct = default) =>
        PatchAsync<AppointmentDto>($"appointments/{id}/complete", request, ct);

    /// <summary>
    /// Completes the appointment and creates its invoice draft in one step.
    /// The appointment is completed even when no invoice could be produced;
    /// inspect <see cref="CompleteAppointmentWithDraftResultDto.DraftCreated"/>
    /// and <c>Reason</c> to report that accurately.
    /// </summary>
    public Task<CompleteAppointmentWithDraftResultDto> CompleteAppointmentWithDraftAsync(
        Guid id,
        CompleteAppointmentRequest? request = null,
        CancellationToken ct = default) =>
        PatchAsync<CompleteAppointmentWithDraftResultDto>($"appointments/{id}/complete-with-draft", request, ct);

    public Task<AppointmentDto> CancelAppointmentAsync(Guid id, CancellationToken ct = default) =>
        PatchAsync<AppointmentDto>($"appointments/{id}/cancel", null, ct);

    public Task<AppointmentDto> MarkNoShowAppointmentAsync(Guid id, CancellationToken ct = default) =>
        PatchAsync<AppointmentDto>($"appointments/{id}/no-show", null, ct);

    // -----------------------------------------------------------------------
    // Invoices
    // -----------------------------------------------------------------------

    public Task<PaginatedList<InvoiceDto>> GetInvoicesAsync(
        int page,
        int pageSize,
        Guid? patientId = null,
        Guid? appointmentId = null,
        CancellationToken ct = default)
    {
        var query = Query(
            ("PageNumber", page),
            ("PageSize", pageSize),
            ("PatientId", patientId),
            ("AppointmentId", appointmentId));

        return GetAsync<PaginatedList<InvoiceDto>>($"invoices?{query}", ct);
    }

    public Task<InvoiceDto> GetInvoiceAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<InvoiceDto>($"invoices/{id}", ct);

    /// <summary>
    /// Server-side totals over <em>all</em> issued invoices visible to the
    /// caller (optionally for one patient) — replaces the wrong client-side
    /// sums over a single page. Draft invoices are estimates and excluded.
    /// </summary>
    public Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(Guid? patientId = null, CancellationToken ct = default) =>
        GetAsync<InvoiceSummaryDto>("invoices/summary?" + Query(("PatientId", patientId)), ct);

    /// <summary>One row per patient with money owed on issued invoices, ordered from the largest debt.</summary>
    public Task<PaginatedList<OutstandingDebtDto>> GetOutstandingDebtsAsync(
        int page,
        int pageSize,
        decimal? minAmount = null,
        CancellationToken ct = default) =>
        GetAsync<PaginatedList<OutstandingDebtDto>>("invoices/outstanding-debts?" + Query(
            ("PageNumber", page), ("PageSize", pageSize), ("MinAmount", minAmount)), ct);

    public Task<InvoiceDto> CreateInvoiceAsync(InvoiceFormRequest request, CancellationToken ct = default) =>
        PostAsync<InvoiceDto>("invoices", request, ct);

    /// <summary>
    /// Generates (or returns the existing) invoice draft for an appointment.
    /// The endpoint is idempotent, so a repeated call will not duplicate the draft.
    /// </summary>
    public Task<GenerateAppointmentInvoiceResultDto> GenerateAppointmentInvoiceAsync(Guid appointmentId, CancellationToken ct = default) =>
        PostAsync<GenerateAppointmentInvoiceResultDto>($"invoices/from-appointment/{appointmentId}", null, ct);

    public Task<InvoiceDto> AddInvoicePaymentAsync(Guid invoiceId, InvoicePaymentRequest request, CancellationToken ct = default) =>
        PostAsync<InvoiceDto>($"invoices/{invoiceId}/payments", request, ct);

    /// <summary>
    /// Returns the charges that would be billed for an appointment, using the same
    /// server-side pricing rules as draft generation.
    /// </summary>
    public Task<AppointmentInvoiceBreakdownDto> GetAppointmentInvoiceBreakdownAsync(Guid appointmentId, CancellationToken ct = default) =>
        GetAsync<AppointmentInvoiceBreakdownDto>($"invoices/breakdown/{appointmentId}", ct);

    /// <summary>
    /// Issues a draft invoice. Safe to call repeatedly; the server treats an
    /// already-issued invoice as a no-op.
    /// </summary>
    public Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, CancellationToken ct = default) =>
        PostAsync<InvoiceDto>($"invoices/{invoiceId}/issue", null, ct);

    /// <summary>
    /// Replaces a draft's line items. The total is recalculated on the server;
    /// only Draft invoices can be edited.
    /// </summary>
    public Task<InvoiceDto> UpdateInvoiceDraftAsync(Guid invoiceId, InvoiceDraftUpdateRequest request, CancellationToken ct = default) =>
        PutAsync<InvoiceDto>($"invoices/{invoiceId}/draft", request, ct);

    /// <summary>
    /// Recalculates a draft from its appointment. Manual adjustments are only
    /// discarded when explicitly confirmed.
    /// </summary>
    public Task<InvoiceDto> RefreshInvoiceDraftAsync(
        Guid invoiceId,
        bool confirmDiscardManualAdjustments = false,
        CancellationToken ct = default) =>
        PostAsync<InvoiceDto>(
            $"invoices/{invoiceId}/draft/refresh",
            new InvoiceDraftRefreshRequest { ConfirmDiscardManualAdjustments = confirmDiscardManualAdjustments },
            ct);

    // -----------------------------------------------------------------------
    // Treatment records
    // -----------------------------------------------------------------------

    public Task<PaginatedList<TreatmentRecordDto>> GetTreatmentRecordsAsync(
        int page,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? appointmentId = null,
        CancellationToken ct = default)
    {
        var query = Query(
            ("PageNumber", page),
            ("PageSize", pageSize),
            ("PatientId", patientId),
            ("DoctorId", doctorId),
            ("AppointmentId", appointmentId));

        return GetAsync<PaginatedList<TreatmentRecordDto>>($"treatment-records?{query}", ct);
    }

    public Task<TreatmentRecordDto> GetTreatmentRecordAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<TreatmentRecordDto>($"treatment-records/{id}", ct);

    public Task<TreatmentRecordDto> CreateTreatmentRecordAsync(TreatmentRecordFormRequest request, CancellationToken ct = default) =>
        PostAsync<TreatmentRecordDto>("treatment-records", request, ct);

    public Task<TreatmentRecordDto> UpdateTreatmentRecordAsync(Guid id, TreatmentRecordFormRequest request, CancellationToken ct = default) =>
        PutAsync<TreatmentRecordDto>($"treatment-records/{id}", request, ct);

    // -----------------------------------------------------------------------
    // Core request pipeline
    // -----------------------------------------------------------------------

    private async Task<T> GetAsync<T>(string url, CancellationToken ct) =>
        await ExecuteAsync<T>(HttpMethod.Get, url, null, ct);

    private async Task<T> PostAsync<T>(string url, object? body, CancellationToken ct) =>
        await ExecuteAsync<T>(HttpMethod.Post, url, body, ct);

    private async Task<T> PutAsync<T>(string url, object? body, CancellationToken ct) =>
        await ExecuteAsync<T>(HttpMethod.Put, url, body, ct);

    private async Task<T> PatchAsync<T>(string url, object? body, CancellationToken ct) =>
        await ExecuteAsync<T>(HttpMethod.Patch, url, body, ct);

    private async Task DeleteAsync(string url, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Delete, url, null, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
        {
            response = await SendAsync(HttpMethod.Delete, url, null, ct);
        }

        await EnsureSuccessAsync(response, ct);
    }

    private async Task<T> ExecuteAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var response = await SendAsync(method, url, body, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(ct))
        {
            response = await SendAsync(method, url, body, ct);
        }

        await EnsureSuccessAsync(response, ct);

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength is 0)
        {
            return default!;
        }

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return value!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, Prefix + url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        if (!string.IsNullOrWhiteSpace(_session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        return await _http.SendAsync(request, ct);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_session.RefreshToken))
        {
            _session.SignOut();
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Prefix + "identity/token/refresh-token")
            {
                Content = JsonContent.Create(
                    new { refreshToken = _session.RefreshToken, expiredAccessToken = _session.AccessToken ?? string.Empty },
                    options: JsonOptions),
            };

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _session.SignOut();
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct);

            if (tokens?.AccessToken is null)
            {
                _session.SignOut();
                return false;
            }

            _session.SetTokens(tokens);
            return true;
        }
        catch
        {
            _session.SignOut();
            return false;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetailsResponse? problem = null;

        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions, ct);
        }
        catch
        {
            // Response had no JSON body – fall back to the status code below.
        }

        var message = problem?.Title
            ?? problem?.Detail
            ?? $"The request failed with status code {(int)response.StatusCode}.";

        if (problem?.Errors is { Count: > 0 })
        {
            var first = problem.Errors.Values
                .SelectMany(values => values)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (first is not null)
            {
                message = first;
            }
        }

        throw new ApiException((int)response.StatusCode, message, problem);
    }

    private static string Query(params (string Key, object? Value)[] pairs)
    {
        var parts = pairs
            .Where(pair => pair.Value is not null && (pair.Value is not string text || !string.IsNullOrWhiteSpace(text)))
            .Select(pair =>
            {
                var value = pair.Value switch
                {
                    Guid guid => guid.ToString(),
                    DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                    bool boolean => boolean ? "true" : "false",
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => pair.Value?.ToString() ?? string.Empty,
                };

                return $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(value)}";
            });

        return string.Join('&', parts);
    }
}
