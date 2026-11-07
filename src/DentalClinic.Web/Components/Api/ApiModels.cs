#pragma warning disable SA1649 // A single aggregate file intentionally groups the REST model types.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalClinic.Web.Api;

// ---------------------------------------------------------------------------
// Shared / framework types
// ---------------------------------------------------------------------------

/// <summary>Shape returned by the API for RFC 7807 problem responses.</summary>
public sealed class ProblemDetailsResponse
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int? Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }

    public string? RequestId { get; set; }
}

/// <summary>Mirror of the API's paginated envelope.</summary>
public sealed class PaginatedList<T>
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public List<T>? Items { get; set; }
}

// ---------------------------------------------------------------------------
// Identity
// ---------------------------------------------------------------------------

public sealed class TokenResponse
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime ExpiresOnUtc { get; set; }
}

public sealed class AppUserDto
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    /// <summary>Linked doctor profile id, when the signed-in account is a doctor.</summary>
    public Guid? DoctorId { get; set; }

    // The API also returns a "claims" collection that the client does not need.
    [JsonIgnore]
    public JsonElement? Claims { get; set; }
}

// ---------------------------------------------------------------------------
// Patients
// ---------------------------------------------------------------------------

public sealed class MedicalHistoryDto
{
    public bool HasDiabetes { get; set; }

    public bool HasHypertension { get; set; }

    public bool HasHeartDisease { get; set; }

    public bool HasBleedingDisorder { get; set; }

    public bool HasEpilepsy { get; set; }

    public bool HasAsthma { get; set; }

    public bool IsPregnant { get; set; }

    public int? PregnancyWeeks { get; set; }

    public string? BloodPressureReading { get; set; }

    public string? Allergies { get; set; }

    public string? Notes { get; set; }

    /// <summary>Server-computed MedicalAlertSeverity (0 = none, 1 = warning, 2 = critical).</summary>
    public int AlertSeverity { get; set; }
}

public sealed class PatientDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PrimaryPhone { get; set; } = string.Empty;

    public string? SecondaryPhone { get; set; }

    public bool HasWhatsAppOnPrimary { get; set; }

    public DateTime DateOfBirth { get; set; }

    public int Gender { get; set; }

    public MedicalHistoryDto MedicalHistory { get; set; } = new();
}

// ---------------------------------------------------------------------------
// Doctors
// ---------------------------------------------------------------------------

public sealed class DoctorDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string PrimaryPhone { get; set; } = string.Empty;

    public string? SecondaryPhone { get; set; }

    public bool HasWhatsAppOnPrimary { get; set; }

    public string? SocialMediaLink { get; set; }

    public int Gender { get; set; }

    public bool IsActive { get; set; }
}

// ---------------------------------------------------------------------------
// Dental services
// ---------------------------------------------------------------------------

public sealed class DentalServiceDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}

// ---------------------------------------------------------------------------
// Appointments
// ---------------------------------------------------------------------------

public enum AppointmentStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4,
}

public sealed class AppointmentDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    public DateTime ScheduledDateTime { get; set; }

    public int DurationInMinutes { get; set; }

    public AppointmentStatus Status { get; set; }

    public string? Notes { get; set; }
}

// ---------------------------------------------------------------------------
// Invoices
// ---------------------------------------------------------------------------

public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
}

public enum InvoiceLifecycleStatus
{
    Draft = 1,
    Issued = 2,
}

public sealed class PaymentDto
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAtUtc { get; set; }

    public string? TransactionNotes { get; set; }
}

public sealed class InvoiceDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable bill identity (<c>INV-2026-0042</c>). Null while the
    /// invoice is still a draft.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public Guid? AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public PaymentStatus Status { get; set; }

    public InvoiceLifecycleStatus LifecycleStatus { get; set; }

    /// <summary>When the invoice became a bill; null for drafts.</summary>
    public DateTimeOffset? IssuedAtUtc { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = [];

    public List<PaymentDto> Payments { get; set; } = [];
}

public sealed class InvoiceLineDto
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

public sealed class GenerateAppointmentInvoiceResultDto
{
    public Guid InvoiceId { get; set; }

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public Guid AppointmentId { get; set; }

    public decimal ServiceSubtotal { get; set; }

    public decimal TreatmentSubtotal { get; set; }

    public decimal TotalAmount { get; set; }

    public PaymentStatus Status { get; set; }

    public InvoiceLifecycleStatus LifecycleStatus { get; set; }

    public bool ReusedExisting { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = [];
}

public sealed class AppointmentInvoiceBreakdownDto
{
    public Guid AppointmentId { get; set; }

    public string? ServiceName { get; set; }

    public decimal ServiceSubtotal { get; set; }

    public decimal TreatmentSubtotal { get; set; }

    public decimal EstimatedTotal { get; set; }

    public int TreatmentCount { get; set; }

    public bool HasBillableItems { get; set; }
}

/// <summary>Server-side money totals over issued invoices; never a client-side sum of one page.</summary>
public sealed class InvoiceSummaryDto
{
    public int TotalInvoiceCount { get; set; }

    public decimal TotalBilled { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalOutstanding { get; set; }

    public int OpenInvoiceCount { get; set; }

    public DateTimeOffset? OldestUnpaidInvoiceDateUtc { get; set; }
}

/// <summary>One debtor per row: money owed on issued invoices, grouped on the server.</summary>
public sealed class OutstandingDebtDto
{
    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int OpenInvoiceCount { get; set; }

    public decimal TotalOutstanding { get; set; }

    public DateTimeOffset? OldestOpenInvoiceDateUtc { get; set; }
}

public sealed class CompleteAppointmentWithDraftResultDto
{
    public Guid AppointmentId { get; set; }

    public Guid? InvoiceId { get; set; }

    public bool DraftCreated { get; set; }

    public bool AlreadyHadInvoice { get; set; }

    public string? Reason { get; set; }
}

// ---------------------------------------------------------------------------
// Treatment records
// ---------------------------------------------------------------------------

public sealed class TreatmentRecordDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    // S8 mirror of the nullable domain value — a treatment may have no tooth.
    public int? ToothNumber { get; set; }

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal Cost { get; set; }
}

public enum TreatmentPlanStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4,
}

public enum TreatmentRecordStatus
{
    Planned = 1,
    Performed = 2,
    Cancelled = 3,
}

public class TreatmentPlanDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public TreatmentPlanStatus Status { get; set; }

    public DateTimeOffset CreatedOnUtc { get; set; }

    public int ItemCount { get; set; }

    public decimal EstimatedTotal { get; set; }

    public decimal PerformedTotal { get; set; }
}

public sealed class TreatmentPlanDetailDto : TreatmentPlanDto
{
    public List<TreatmentPlanItemDto> Items { get; set; } = [];
}

public sealed class TreatmentPlanItemDto
{
    public Guid Id { get; set; }

    public Guid? PlanId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    public int? ToothNumber { get; set; }

    public string ProcedureDetails { get; set; } = string.Empty;

    public TreatmentRecordStatus Status { get; set; }

    public decimal Cost { get; set; }

    public decimal? EstimatedCost { get; set; }

    public int SessionNumber { get; set; }
}

public sealed class TreatmentPlanSessionDto
{
    public int SessionNumber { get; set; }

    public int ItemCount { get; set; }

    public decimal EstimatedTotal { get; set; }

    public decimal PerformedTotal { get; set; }

    public decimal GrandTotal { get; set; }
}

// ---------------------------------------------------------------------------
// Odontograms
// ---------------------------------------------------------------------------

public enum ToothCondition
{
    Healthy,
    Caries,
    Filled,
    Crown,
    Bridge,
    Implant,
    Extracted,
    Missing,
}

public enum ToothSurface
{
    Occlusal,
    Mesial,
    Distal,
    Buccal,
    Lingual,
    Incisal,
}

public sealed class PatientOdontogramDto
{
    public Guid PatientId { get; set; }

    public List<PatientToothSummaryDto> Teeth { get; set; } = [];

    public List<ToothHistoryDto> History { get; set; } = [];
}

public sealed class PatientToothSummaryDto
{
    public int ToothNumber { get; set; }

    public ToothCondition Condition { get; set; }

    public ToothSurface? Surface { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? LastModifiedUtc { get; set; }
}

public sealed class ToothHistoryDto
{
    // S8 mirror of the nullable domain value — null = not tied to a tooth;
    // the odontogram's per-tooth grouping skips such entries.
    public int? ToothNumber { get; set; }

    public Guid TreatmentRecordId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public Guid DoctorId { get; set; }

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal Cost { get; set; }
}

// ---------------------------------------------------------------------------
// Request payloads
// ---------------------------------------------------------------------------

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class ContactInfoRequest
{
    public string PrimaryPhone { get; set; } = string.Empty;

    public string? SecondaryPhone { get; set; }

    public bool HasWhatsAppOnPrimary { get; set; } = true;

    public string? SocialMediaLink { get; set; }
}

public sealed class MedicalHistoryRequest
{
    public bool HasDiabetes { get; set; }

    public bool HasHypertension { get; set; }

    public bool HasHeartDisease { get; set; }

    public bool HasBleedingDisorder { get; set; }

    public bool HasEpilepsy { get; set; }

    public bool HasAsthma { get; set; }

    public bool IsPregnant { get; set; }

    public int? PregnancyWeeks { get; set; }

    public string? BloodPressureReading { get; set; }

    public string? Allergies { get; set; }

    public string? Notes { get; set; }
}

public sealed class PatientFormRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-30);

    public string Gender { get; set; } = "Male";

    public ContactInfoRequest ContactInfo { get; set; } = new();

    public MedicalHistoryRequest MedicalHistory { get; set; } = new();
}

public sealed class DoctorFormRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string Gender { get; set; } = "Male";

    public ContactInfoRequest ContactInfo { get; set; } = new();
}

public sealed class AppointmentFormRequest
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? ServiceId { get; set; }

    public DateTime ScheduledDateTime { get; set; } = DateTime.Now.AddHours(2);

    public int DurationInMinutes { get; set; } = 30;

    public string? Notes { get; set; }
}

public sealed class RescheduleAppointmentRequest
{
    public DateTime NewScheduledDateTime { get; set; } = DateTime.Now.AddDays(1);

    public int DurationInMinutes { get; set; } = 30;
}

public sealed class CompleteAppointmentRequest
{
    public List<CompleteAppointmentTreatmentRequest>? Treatments { get; set; }

    public Guid? ServiceId { get; set; }
}

public sealed class CompleteAppointmentTreatmentRequest
{
    // S8 mirror — null = not linked to a specific tooth.
    public int? ToothNumber { get; set; } = 11;

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal Cost { get; set; }
}

public sealed class DentalServiceFormRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }
}

public sealed class InvoiceFormRequest
{
    public Guid PatientId { get; set; }

    public Guid? AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }
}

public sealed class InvoicePaymentRequest
{
    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}

public sealed class InvoiceLineFormRequest
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

public sealed class InvoiceDraftUpdateRequest
{
    public List<InvoiceLineFormRequest> Lines { get; set; } = [];
}

public sealed class InvoiceDraftRefreshRequest
{
    public bool ConfirmDiscardManualAdjustments { get; set; }
}

public sealed class TreatmentRecordFormRequest
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    // S8 mirror — null = not linked to a specific tooth.
    public int? ToothNumber { get; set; } = 11;

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal Cost { get; set; }
}

public sealed class TreatmentPlanFormRequest
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public sealed class TreatmentPlanItemFormRequest
{
    public int? ToothNumber { get; set; }

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal? EstimatedCost { get; set; }

    public int SessionNumber { get; set; } = 1;
}

public sealed class PerformTreatmentPlanItemRequest
{
    public decimal ActualCost { get; set; }

    public Guid? AppointmentId { get; set; }
}

public sealed class UpsertToothConditionRequest
{
    public ToothCondition Condition { get; set; }

    public ToothSurface? Surface { get; set; }

    public string? Notes { get; set; }
}

public sealed class ClinicSettingsDto
{
    public string Name { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public string BaseCurrency { get; set; } = string.Empty;

    public string? SecondaryCurrency { get; set; }

    public string? DefaultCountryCode { get; set; }
}

public sealed class ClinicSettingsFormRequest
{
    public string Name { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public string BaseCurrency { get; set; } = string.Empty;

    public string? SecondaryCurrency { get; set; }

    public string? DefaultCountryCode { get; set; }
}

/// <summary>
/// The conversion rate in force. <see cref="Rate"/> reads as "one unit of
/// <see cref="FromCurrency"/> = Rate units of <see cref="ToCurrency"/>".
/// </summary>
public sealed class ExchangeRateDto
{
    public Guid Id { get; set; }

    public string FromCurrency { get; set; } = string.Empty;

    public string ToCurrency { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public DateTimeOffset EffectiveFromUtc { get; set; }
}

public sealed class ExchangeRateFormRequest
{
    public string FromCurrency { get; set; } = string.Empty;

    public string ToCurrency { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public DateTimeOffset? EffectiveFromUtc { get; set; }
}

// ---------------------------------------------------------------------------
// Reports
// ---------------------------------------------------------------------------

/// <summary>
/// The dashboard and reports aggregate, computed server-side in the clinic's
/// timezone. <see cref="Revenue"/> is the invoice cohort issued inside the
/// window, <see cref="Outstanding"/> the point-in-time balance on the books and
/// <see cref="CashCollected"/> the payments received inside the window.
/// </summary>
public sealed class DashboardSummaryDto
{
    public int TotalPatients { get; set; }

    public int TotalDoctors { get; set; }

    public int TotalServices { get; set; }

    public int PatientsWithMedicalAlerts { get; set; }

    public DateOnly Today { get; set; }

    public int TodayAppointmentCount { get; set; }

    public int TodayScheduledCount { get; set; }

    public List<DashboardAppointmentDto> TodayAppointments { get; set; } = [];

    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public InvoiceSummaryDto Revenue { get; set; } = new();

    public InvoiceSummaryDto Outstanding { get; set; } = new();

    public decimal CashCollected { get; set; }

    public List<AppointmentStatusCountDto> AppointmentStatusBreakdown { get; set; } = [];

    public List<DashboardAppointmentDto> UpcomingAppointments { get; set; } = [];

    public List<DashboardInvoiceDto> RecentInvoices { get; set; } = [];

    public List<DashboardPatientDto> NewestPatients { get; set; } = [];

    public List<DoctorProductivityDto> DoctorProductivity { get; set; } = [];
}

/// <summary>One appointment row with the patient, doctor and service names already resolved.</summary>
public sealed class DashboardAppointmentDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string? ServiceName { get; set; }

    public DateTime ScheduledDateTime { get; set; }

    public int DurationInMinutes { get; set; }

    public AppointmentStatus Status { get; set; }
}

/// <summary>One invoice row with the patient name resolved and the balance computed in SQL.</summary>
public sealed class DashboardInvoiceDto
{
    public Guid Id { get; set; }

    public string? InvoiceNumber { get; set; }

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public PaymentStatus Status { get; set; }

    public InvoiceLifecycleStatus LifecycleStatus { get; set; }
}

/// <summary>One patient row from the newest-registrations list.</summary>
public sealed class DashboardPatientDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string? PrimaryPhone { get; set; }
}

/// <summary>How many appointments sat in one status inside the report window.</summary>
public sealed class AppointmentStatusCountDto
{
    public AppointmentStatus Status { get; set; }

    public int Count { get; set; }
}

/// <summary>What one doctor did inside the report window.</summary>
public sealed class DoctorProductivityDto
{
    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int AppointmentCount { get; set; }

    public int IssuedInvoiceCount { get; set; }

    public decimal BilledAmount { get; set; }

    public decimal PaidAmount { get; set; }
}