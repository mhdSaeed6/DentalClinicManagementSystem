using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;

namespace DentalClinic.Domain.TreatmentRecords;

public class TreatmentRecord : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? AppointmentId { get; private set; }

    // Navigation Properties
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public Appointment? Appointment { get; set; }

    // قابل للـ null عمداً: بعض العلاجات لا ترتبط بسن محدد (استشارة عامة مثلًا).
    // S8 — nullable ToothNumber; a provided value must still be a valid FDI tooth.
    public int? ToothNumber { get; private set; }
    public string ProcedureDetails { get; private set; } = default!;
    public decimal Cost { get; private set; }

    // P1.1 — treatment plans (Option A). Null for ad-hoc treatments; set for
    // items created through a plan (they are the plan's line items).
    public Guid? PlanId { get; private set; }
    public TreatmentPlan? Plan { get; set; }

    // Planned only for items created through a plan; direct creation is
    // performed work by definition (D-P3).
    public TreatmentRecordStatus Status { get; private set; }

    // Plan-time estimate — never billed. Cost below stays the actual charged
    // amount (D-P2); the two are kept separate so an estimate can never leak
    // onto an invoice.
    public decimal? EstimatedCost { get; private set; }

    // 1-based; groups items for per-session totals and the print sheet.
    public int SessionNumber { get; private set; }

    // Only performed work with an actual cost is billable — the invoice
    // pipeline filters on this state.
    public bool IsBillable => Status == TreatmentRecordStatus.Performed && Cost > 0;

    private TreatmentRecord() { }

    private TreatmentRecord(
        Guid id,
        Guid patientId,
        Guid doctorId,
        Guid? appointmentId,
        int? toothNumber,
        string procedureDetails,
        decimal cost)
        : this(
            id,
            patientId,
            doctorId,
            appointmentId,
            toothNumber,
            procedureDetails,
            cost,
            planId: null,
            status: TreatmentRecordStatus.Performed,
            estimatedCost: null,
            sessionNumber: 1)
    {
    }

    private TreatmentRecord(
        Guid id,
        Guid patientId,
        Guid doctorId,
        Guid? appointmentId,
        int? toothNumber,
        string procedureDetails,
        decimal cost,
        Guid? planId,
        TreatmentRecordStatus status,
        decimal? estimatedCost,
        int sessionNumber)
        : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        AppointmentId = appointmentId;
        ToothNumber = toothNumber;
        ProcedureDetails = procedureDetails;
        Cost = cost;
        PlanId = planId;
        Status = status;
        EstimatedCost = estimatedCost;
        SessionNumber = sessionNumber;
    }

    public static Result<TreatmentRecord> Create(
        Guid patientId,
        Guid doctorId,
        int? toothNumber,
        string procedureDetails,
        decimal cost,
        Guid? appointmentId = null)
    {
        // 1. التحقق من رقم السن عبر FdiTooth — null مسموح (علاج غير مرتبط بسن محدد)
        if (toothNumber.HasValue && !IsValidToothNumber(toothNumber.Value))
        {
            return TreatmentRecordErrors.InvalidToothNumber;
        }

        // 2. التحقق من التفاصيل والتكلفة
        if (string.IsNullOrWhiteSpace(procedureDetails))
        {
            return TreatmentRecordErrors.ProcedureDetailsRequired;
        }

        if (patientId == Guid.Empty)
        {
            return TreatmentRecordErrors.PatientIdRequired;
        }

        if (doctorId == Guid.Empty)
        {
            return TreatmentRecordErrors.DoctorIdRequired;
        }

        if (cost < 0)
        {
            return TreatmentRecordErrors.InvalidCost;
        }

        // D-P3: a directly created record is performed work — never Planned.
        return new TreatmentRecord(
            Guid.NewGuid(),
            patientId,
            doctorId,
            appointmentId,
            toothNumber,
            procedureDetails.Trim(),
            cost);
    }

    // إنشاء عنصر مخطط داخل خطة علاج — لا يُفوترة حتى يُنفّذ عبر MarkPerformed.
    // Creates a planned plan item; billing waits until the item is performed.
    public static Result<TreatmentRecord> CreatePlanItem(
        Guid planId,
        Guid patientId,
        Guid doctorId,
        int? toothNumber,
        string procedureDetails,
        decimal? estimatedCost,
        int sessionNumber = 1)
    {
        if (planId == Guid.Empty)
        {
            return TreatmentRecordErrors.PlanIdRequired;
        }

        // 1. التحقق من رقم السن — null مسموح (عنصر غير مرتبط بسن محدد)
        if (toothNumber.HasValue && !IsValidToothNumber(toothNumber.Value))
        {
            return TreatmentRecordErrors.InvalidToothNumber;
        }

        if (string.IsNullOrWhiteSpace(procedureDetails))
        {
            return TreatmentRecordErrors.ProcedureDetailsRequired;
        }

        if (patientId == Guid.Empty)
        {
            return TreatmentRecordErrors.PatientIdRequired;
        }

        if (doctorId == Guid.Empty)
        {
            return TreatmentRecordErrors.DoctorIdRequired;
        }

        if (estimatedCost.HasValue && estimatedCost.Value < 0)
        {
            return TreatmentRecordErrors.InvalidEstimatedCost;
        }

        if (sessionNumber < 1)
        {
            return TreatmentRecordErrors.InvalidSessionNumber;
        }

        return new TreatmentRecord(
            Guid.NewGuid(),
            patientId,
            doctorId,
            appointmentId: null,
            toothNumber,
            procedureDetails.Trim(),
            cost: 0,
            planId,
            TreatmentRecordStatus.Planned,
            estimatedCost,
            sessionNumber);
    }

    public Result<Updated> Update(
        Guid patientId,
        Guid doctorId,
        int? toothNumber,
        string procedureDetails,
        decimal cost,
        Guid? appointmentId = null)
    {
        // 1. التحقق من رقم السن عبر FdiTooth — null مسموح (يمسح الارتباط بالسن)
        if (toothNumber.HasValue && !IsValidToothNumber(toothNumber.Value))
        {
            return TreatmentRecordErrors.InvalidToothNumber;
        }

        // 2. التفاصيل والتكلفة
        if (string.IsNullOrWhiteSpace(procedureDetails))
        {
            return TreatmentRecordErrors.ProcedureDetailsRequired;
        }

        if (patientId == Guid.Empty)
        {
            return TreatmentRecordErrors.PatientIdRequired;
        }

        if (doctorId == Guid.Empty)
        {
            return TreatmentRecordErrors.DoctorIdRequired;
        }

        if (cost < 0)
        {
            return TreatmentRecordErrors.InvalidCost;
        }

        PatientId = patientId;
        DoctorId = doctorId;
        ToothNumber = toothNumber;
        ProcedureDetails = procedureDetails.Trim();
        AppointmentId = appointmentId;
        Cost = cost;

        return Result.Updated;
    }

    // تعديل تفاصيل العنصر المخطط (السن/التقدير/رقم الجلسة) — فقط قبل التنفيذ.
    // Plan-side edits (estimate, session) are allowed only while planned;
    // performed work is corrected through Update above.
    public Result<Updated> UpdatePlanItem(
        int? toothNumber,
        string procedureDetails,
        decimal? estimatedCost,
        int sessionNumber)
    {
        if (Status is not TreatmentRecordStatus.Planned)
        {
            return TreatmentRecordErrors.ItemNotPlanned;
        }

        if (toothNumber.HasValue && !IsValidToothNumber(toothNumber.Value))
        {
            return TreatmentRecordErrors.InvalidToothNumber;
        }

        if (string.IsNullOrWhiteSpace(procedureDetails))
        {
            return TreatmentRecordErrors.ProcedureDetailsRequired;
        }

        if (estimatedCost.HasValue && estimatedCost.Value < 0)
        {
            return TreatmentRecordErrors.InvalidEstimatedCost;
        }

        if (sessionNumber < 1)
        {
            return TreatmentRecordErrors.InvalidSessionNumber;
        }

        ToothNumber = toothNumber;
        ProcedureDetails = procedureDetails.Trim();
        EstimatedCost = estimatedCost;
        SessionNumber = sessionNumber;

        return Result.Updated;
    }

    // تنفيذ عنصر مخطط: التقدير يتحول إلى تكلفة فعلية، والعنصر يصبح قابلاً للفوترة.
    // Planned → Performed: the actual charged cost replaces the silent estimate.
    public Result<Updated> MarkPerformed(decimal actualCost, Guid? appointmentId = null)
    {
        if (actualCost < 0)
        {
            return TreatmentRecordErrors.InvalidCost;
        }

        if (Status is TreatmentRecordStatus.Performed)
        {
            return TreatmentRecordErrors.ItemAlreadyPerformed;
        }

        if (Status is TreatmentRecordStatus.Cancelled)
        {
            return TreatmentRecordErrors.ItemCancelled;
        }

        Cost = actualCost;

        if (appointmentId.HasValue)
        {
            AppointmentId = appointmentId;
        }

        Status = TreatmentRecordStatus.Performed;

        return Result.Updated;
    }

    // إلغاء العنصر: يبقى في السجل وفق قاعدة "الملغي يظهر ويُدرج دائماً".
    // A cancelled item never disappears from history and is never billable.
    public Result<Updated> Cancel()
    {
        if (Status is TreatmentRecordStatus.Cancelled)
        {
            return TreatmentRecordErrors.ItemAlreadyCancelled;
        }

        Status = TreatmentRecordStatus.Cancelled;

        return Result.Updated;
    }

    // D-P4: إعادة فتح عنصر مُنفّذ إلى مخطط — يُمنع في طبقة التطبيق إذا
    // كان ظاهراً في فاتورة صادرة؛ هنا يبقى التحقق على النقل الحالات فقط.
    // D-P4: performed → planned reopen; the "already on an issued invoice"
    // guard is enforced in the application layer where invoices are visible.
    public Result<Updated> Reopen()
    {
        if (Status is not TreatmentRecordStatus.Performed)
        {
            return TreatmentRecordErrors.ItemNotPerformed;
        }

        Status = TreatmentRecordStatus.Planned;

        return Result.Updated;
    }

    // Helper Method للتحقق من أرقام الأسنان المقبولة (FDI).
    // Public so application validators reuse the single source of truth.
    // Delegates to FdiTooth — the single canonical copy of FDI validation.
    public static bool IsValidToothNumber(int number)
    {
        return FdiTooth.IsValidToothNumber(number);
    }
}