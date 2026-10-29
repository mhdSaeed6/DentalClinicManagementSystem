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

    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public Guid? AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public PaymentStatus Status { get; set; }

    public InvoiceLifecycleStatus LifecycleStatus { get; set; }

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

public sealed class UpsertToothConditionRequest
{
    public ToothCondition Condition { get; set; }

    public ToothSurface? Surface { get; set; }

    public string? Notes { get; set; }
}
