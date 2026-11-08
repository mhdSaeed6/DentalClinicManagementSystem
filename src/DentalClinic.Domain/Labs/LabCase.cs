using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Domain.Labs;

public class LabCase : AuditableEntity
{
    /// <summary>
    /// Status transitions of the lab workflow. Keys are current statuses, values
    /// are the statuses reachable from them. <c>Failed → Sent</c> is the remake
    /// path: the same case row is re-sent instead of being duplicated.
    /// </summary>
    private static readonly Dictionary<LabCaseStatus, LabCaseStatus[]> s_allowed = new()
    {
        [LabCaseStatus.Sent] = [LabCaseStatus.AtLab, LabCaseStatus.Failed],
        [LabCaseStatus.AtLab] = [LabCaseStatus.Trial, LabCaseStatus.Returned, LabCaseStatus.Failed],
        [LabCaseStatus.Trial] = [LabCaseStatus.TriedIn, LabCaseStatus.Failed],
        [LabCaseStatus.TriedIn] = [LabCaseStatus.Returned, LabCaseStatus.Failed],
        [LabCaseStatus.Returned] = [],
        [LabCaseStatus.Failed] = [LabCaseStatus.Sent],
    };

    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? TreatmentRecordId { get; private set; }

    // Navigation Properties
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public TreatmentRecord? TreatmentRecord { get; set; }

    // Nullable on purpose (S8 convention): some lab work is not tied to a
    // specific tooth (e.g. a full denture), and a provided value must still be
    // a valid FDI tooth.
    public int? ToothNumber { get; private set; }

    public string LabName { get; private set; } = default!;
    public string CaseType { get; private set; } = default!;
    public LabCaseStatus Status { get; private set; }

    public DateTimeOffset SentAtUtc { get; private set; }
    public DateTimeOffset ExpectedReturnAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }

    // Lab cost (18, 2 in the database) — informational for now: it is shown on
    // the tracking page and traces. Recharging it onto the patient's invoice
    // (item 3) is deliberately left for the invoice pipeline to consume.
    public decimal Cost { get; private set; }
    public string? Notes { get; private set; }

    private LabCase() { }

    private LabCase(
        Guid id,
        Guid patientId,
        Guid doctorId,
        Guid? treatmentRecordId,
        int? toothNumber,
        string labName,
        string caseType,
        DateTimeOffset sentAtUtc,
        DateTimeOffset expectedReturnAtUtc,
        decimal cost,
        string? notes)
        : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        TreatmentRecordId = treatmentRecordId;
        ToothNumber = toothNumber;
        LabName = labName;
        CaseType = caseType;
        Status = LabCaseStatus.Sent;
        SentAtUtc = sentAtUtc;
        ExpectedReturnAtUtc = expectedReturnAtUtc;
        Cost = cost;
        Notes = notes;
    }

    public static Result<LabCase> Create(
        Guid patientId,
        Guid doctorId,
        string labName,
        string caseType,
        DateTimeOffset sentAtUtc,
        DateTimeOffset expectedReturnAtUtc,
        Guid? treatmentRecordId = null,
        int? toothNumber = null,
        decimal cost = 0,
        string? notes = null)
    {
        // 1. المشاركون الأساسيون — المريض والطبيب مطلوبان دائماً.
        if (patientId == Guid.Empty)
        {
            return LabCaseErrors.PatientIdRequired;
        }

        if (doctorId == Guid.Empty)
        {
            return LabCaseErrors.DoctorIdRequired;
        }

        // 2. بيانات المخبر والنوع
        if (string.IsNullOrWhiteSpace(labName))
        {
            return LabCaseErrors.LabNameRequired;
        }

        if (string.IsNullOrWhiteSpace(caseType))
        {
            return LabCaseErrors.CaseTypeRequired;
        }

        // 3. رقم السن عبر FdiTooth — null مسموح (عمل غير مرتبط بسن محدد)
        if (toothNumber.HasValue && !FdiTooth.IsValidToothNumber(toothNumber.Value))
        {
            return LabCaseErrors.InvalidToothNumber;
        }

        if (cost < 0)
        {
            return LabCaseErrors.InvalidCost;
        }

        // 4. التواريخ المخططة: لا يمكن أن يكون الاسترجاع قبل الإرسال.
        if (expectedReturnAtUtc < sentAtUtc)
        {
            return LabCaseErrors.InvalidPlannedDates;
        }

        return new LabCase(
            Guid.NewGuid(),
            patientId,
            doctorId,
            treatmentRecordId,
            toothNumber,
            labName.Trim(),
            caseType.Trim(),
            sentAtUtc,
            expectedReturnAtUtc,
            cost,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
    }

    public Result<Updated> Update(
        string labName,
        string caseType,
        DateTimeOffset expectedReturnAtUtc,
        int? toothNumber,
        decimal cost,
        string? notes,
        Guid? treatmentRecordId)
    {
        // الحالة النهائية: حالة مُسلَّمة أو فاشلة لا تُعدَّل بياناتها التشغيلية.
        if (Status is LabCaseStatus.Returned or LabCaseStatus.Failed)
        {
            return LabCaseErrors.CaseClosed;
        }

        if (string.IsNullOrWhiteSpace(labName))
        {
            return LabCaseErrors.LabNameRequired;
        }

        if (string.IsNullOrWhiteSpace(caseType))
        {
            return LabCaseErrors.CaseTypeRequired;
        }

        if (toothNumber.HasValue && !FdiTooth.IsValidToothNumber(toothNumber.Value))
        {
            return LabCaseErrors.InvalidToothNumber;
        }

        if (cost < 0)
        {
            return LabCaseErrors.InvalidCost;
        }

        if (expectedReturnAtUtc < SentAtUtc)
        {
            return LabCaseErrors.InvalidPlannedDates;
        }

        LabName = labName.Trim();
        CaseType = caseType.Trim();
        ExpectedReturnAtUtc = expectedReturnAtUtc;
        ToothNumber = toothNumber;
        Cost = cost;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        TreatmentRecordId = treatmentRecordId;

        return Result.Updated;
    }

    /// <summary>
    /// Advances the case through the lab workflow using the explicit transition
    /// map. <paramref name="occurredAtUtc"/> stamps the transition: it becomes
    /// the return date on <c>Returned</c> and re-stamps the send date on the
    /// <c>Failed → Sent</c> remake path.
    /// </summary>
    public Result<Updated> ChangeStatus(LabCaseStatus newStatus, DateTimeOffset occurredAtUtc)
    {
        if (Status == newStatus)
        {
            return LabCaseErrors.StatusAlreadySet;
        }

        if (Status != LabCaseStatus.Returned && occurredAtUtc < SentAtUtc)
        {
            return LabCaseErrors.InvalidReturnedAt;
        }

        if (!s_allowed.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            return LabCaseErrors.InvalidStatusTransition;
        }

        Status = newStatus;

        if (newStatus == LabCaseStatus.Returned)
        {
            ReturnedAtUtc = occurredAtUtc;
        }
        else if (newStatus == LabCaseStatus.Sent)
        {
            // Remake: the case restarts its lifecycle from the new send date.
            SentAtUtc = occurredAtUtc;
            ReturnedAtUtc = null;
        }

        return Result.Updated;
    }

    /// <summary>
    /// True when the lab was expected to return the case before
    /// <paramref name="asOfUtc"/> and the case is still open. Returned and
    /// failed cases are never overdue regardless of the dates.
    /// </summary>
    public bool IsOverdue(DateTimeOffset asOfUtc) =>
        Status is not (LabCaseStatus.Returned or LabCaseStatus.Failed)
        && asOfUtc > ExpectedReturnAtUtc;
}