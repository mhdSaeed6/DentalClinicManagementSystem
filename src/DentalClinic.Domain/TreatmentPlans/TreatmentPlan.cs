using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Domain.TreatmentPlans;

// رأس خطة العلاج — سجل واحد يجمع جلسات متعددة مع تقديرات لكل عنصر،
// ويمكن طباعته كورقة تقدير موقعة للمريض.
// Option A (D-P1): the plan header owns only identity, schedule and status —
// its line items ARE TreatmentRecords (TreatmentRecord.PlanId), so history,
// the odontogram and invoicing all stay in one clinical fact table.
public class TreatmentPlan : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Notes { get; private set; }
    public TreatmentPlanStatus Status { get; private set; }

    // Navigation Properties
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }

    private TreatmentPlan() { }

    private TreatmentPlan(
        Guid id,
        Guid patientId,
        Guid doctorId,
        string title,
        string? notes,
        TreatmentPlanStatus status)
        : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        Title = title;
        Notes = notes;
        Status = status;
    }

    public static Result<TreatmentPlan> Create(
        Guid patientId,
        Guid doctorId,
        string title,
        string? notes = null)
    {
        // 1. التحقق من المريض والطبيب
        if (patientId == Guid.Empty)
        {
            return TreatmentPlanErrors.PatientIdRequired;
        }

        if (doctorId == Guid.Empty)
        {
            return TreatmentPlanErrors.DoctorIdRequired;
        }

        // 2. العنوان مطلوب، والملاحظات اختيارية
        if (string.IsNullOrWhiteSpace(title))
        {
            return TreatmentPlanErrors.TitleRequired;
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 200)
        {
            return TreatmentPlanErrors.TitleTooLong;
        }

        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is not null && trimmedNotes.Length > 1000)
        {
            return TreatmentPlanErrors.NotesTooLong;
        }

        return new TreatmentPlan(
            Guid.NewGuid(),
            patientId,
            doctorId,
            trimmedTitle,
            trimmedNotes,
            TreatmentPlanStatus.Draft);
    }

    public Result<Updated> Update(string title, string? notes = null)
    {
        // لا يمكن تعديل خطة منجزة أو ملغاة — الطباعة والتقدير النهائي لا يعودان صالحين.
        if (Status is TreatmentPlanStatus.Cancelled)
        {
            return TreatmentPlanErrors.CannotModifyCancelledPlan;
        }

        if (Status is TreatmentPlanStatus.Completed)
        {
            return TreatmentPlanErrors.CannotModifyCompletedPlan;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return TreatmentPlanErrors.TitleRequired;
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 200)
        {
            return TreatmentPlanErrors.TitleTooLong;
        }

        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is not null && trimmedNotes.Length > 1000)
        {
            return TreatmentPlanErrors.NotesTooLong;
        }

        Title = trimmedTitle;
        Notes = trimmedNotes;

        return Result.Updated;
    }

    // Draft → Active: تبدأ الخطة في التنفيذ جلسة بعد جلسة.
    public Result<Updated> Activate()
    {
        if (Status is TreatmentPlanStatus.Cancelled)
        {
            return TreatmentPlanErrors.CannotModifyCancelledPlan;
        }

        if (Status is TreatmentPlanStatus.Completed)
        {
            return TreatmentPlanErrors.CannotModifyCompletedPlan;
        }

        if (Status is TreatmentPlanStatus.Active)
        {
            return TreatmentPlanErrors.PlanAlreadyActive;
        }

        Status = TreatmentPlanStatus.Active;

        return Result.Updated;
    }

    // Active → Completed: تُستوفى جميع الجلسات وتُغلق الخطة نهائياً.
    public Result<Updated> Complete()
    {
        if (Status is TreatmentPlanStatus.Cancelled)
        {
            return TreatmentPlanErrors.CannotModifyCancelledPlan;
        }

        if (Status is TreatmentPlanStatus.Completed)
        {
            return TreatmentPlanErrors.PlanAlreadyCompleted;
        }

        if (Status is TreatmentPlanStatus.Draft)
        {
            return TreatmentPlanErrors.PlanNotActive;
        }

        Status = TreatmentPlanStatus.Completed;

        return Result.Updated;
    }

    // Draft/Active → Cancelled: الخطة تلغى، لكن عناصرها تبقى في السجل.
    // Cancelling the plan does not touch its items; planned items stay silent
    // on invoices and performed items stay in history.
    public Result<Updated> Cancel()
    {
        if (Status is TreatmentPlanStatus.Cancelled)
        {
            return TreatmentPlanErrors.PlanAlreadyCancelled;
        }

        if (Status is TreatmentPlanStatus.Completed)
        {
            return TreatmentPlanErrors.PlanAlreadyCompleted;
        }

        Status = TreatmentPlanStatus.Cancelled;

        return Result.Updated;
    }

    // Delete موروث من AuditableEntity (حذف ناعم) — لا حذف صلب أبداً (D-P5).
    // Delete is inherited from AuditableEntity (soft delete); the explicit
    // warning for plans with performed items lives in the application commands.
}