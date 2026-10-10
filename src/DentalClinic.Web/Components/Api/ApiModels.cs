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

    public string? Allergies { get; set; }

    public string? Notes { get; set; }
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

    public Guid ServiceId { get; set; }

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

    public Guid? AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public PaymentStatus Status { get; set; }

    public List<PaymentDto> Payments { get; set; } = [];
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

    public int ToothNumber { get; set; }

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

    public Guid ServiceId { get; set; }

    public DateTime ScheduledDateTime { get; set; } = DateTime.Now.AddHours(2);

    public int DurationInMinutes { get; set; } = 30;

    public string? Notes { get; set; }
}

public sealed class RescheduleAppointmentRequest
{
    public DateTime NewScheduledDateTime { get; set; } = DateTime.Now.AddDays(1);

    public int DurationInMinutes { get; set; } = 30;
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

public sealed class TreatmentRecordFormRequest
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    public int ToothNumber { get; set; } = 11;

    public string ProcedureDetails { get; set; } = string.Empty;

    public decimal Cost { get; set; }
}
