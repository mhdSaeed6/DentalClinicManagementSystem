using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Domain.Odontograms;

public class PatientTooth : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public int ToothNumber { get; private set; }

    // الحالة السريرية للسن الواحد (FDI) — صف واحد حي على الأكثر لكل (مريض، سن).
    public ToothCondition Condition { get; private set; }
    public ToothSurface? Surface { get; private set; }
    public string? Notes { get; private set; }

    // Navigation Properties
    public Patient? Patient { get; set; }

    private PatientTooth() { }

    private PatientTooth(
        Guid id,
        Guid patientId,
        int toothNumber,
        ToothCondition condition,
        ToothSurface? surface,
        string? notes)
        : base(id)
    {
        PatientId = patientId;
        ToothNumber = toothNumber;
        Condition = condition;
        Surface = surface;
        Notes = notes;
    }

    public static Result<PatientTooth> Create(
        Guid patientId,
        int toothNumber,
        ToothCondition condition,
        ToothSurface? surface = null,
        string? notes = null)
    {
        // 1. التحقق من المريض
        if (patientId == Guid.Empty)
        {
            return PatientToothErrors.PatientIdRequired;
        }

        // 2. التحقق من رقم السن عبر FdiTooth
        if (!FdiTooth.IsValidToothNumber(toothNumber))
        {
            return PatientToothErrors.InvalidToothNumber;
        }

        // 3. التحقق من الحالة
        if (!Enum.IsDefined(condition))
        {
            return PatientToothErrors.InvalidCondition;
        }

        // 4. التحقق من سطح السن (اختياري لكن يجب أن يكون قيمة معرَّفة)
        if (surface is not null && !Enum.IsDefined(surface.Value))
        {
            return PatientToothErrors.InvalidSurface;
        }

        // 5. التحقق من الملاحظات (الحد الأقصى 500 حرف بعد التنظيف)
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is not null && trimmedNotes.Length > 500)
        {
            return PatientToothErrors.NotesTooLong;
        }

        return new PatientTooth(
            Guid.NewGuid(),
            patientId,
            toothNumber,
            condition,
            surface,
            trimmedNotes);
    }

    public Result<Updated> Update(
        ToothCondition condition,
        ToothSurface? surface = null,
        string? notes = null)
    {
        // 1. التحقق من الحالة
        if (!Enum.IsDefined(condition))
        {
            return PatientToothErrors.InvalidCondition;
        }

        // 2. التحقق من سطح السن (اختياري لكن يجب أن يكون قيمة معرَّفة)
        if (surface is not null && !Enum.IsDefined(surface.Value))
        {
            return PatientToothErrors.InvalidSurface;
        }

        // 3. التحقق من الملاحظات (الحد الأقصى 500 حرف بعد التنظيف)
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is not null && trimmedNotes.Length > 500)
        {
            return PatientToothErrors.NotesTooLong;
        }

        Condition = condition;
        Surface = surface;
        Notes = trimmedNotes;

        return Result.Updated;
    }

    // Delete موروث من AuditableEntity (حذف ناعم) — لا حاجة لإعادة تعريفه هنا.
    // Delete is inherited from AuditableEntity (soft delete) — do not redefine it.
}