using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;

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

    public int ToothNumber { get; private set; }
    public string ProcedureDetails { get; private set; } = default!;
    public decimal Cost { get; private set; }

    private TreatmentRecord() { }

    private TreatmentRecord(
        Guid id,
        Guid patientId,
        Guid doctorId,
        Guid? appointmentId,
        int toothNumber,
        string procedureDetails,
        decimal cost)
        : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        AppointmentId = appointmentId;
        ToothNumber = toothNumber;
        ProcedureDetails = procedureDetails;
        Cost = cost;
    }

    public static Result<TreatmentRecord> Create(
        Guid patientId,
        Guid doctorId,
        int toothNumber,
        string procedureDetails,
        decimal cost,
        Guid? appointmentId = null)
    {
        // 1. التحقق من رقم السن عبر Private Method
        if (!IsValidToothNumber(toothNumber))
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

        return new TreatmentRecord(
            Guid.NewGuid(),
            patientId,
            doctorId,
            appointmentId,
            toothNumber,
            procedureDetails.Trim(),
            cost);
    }

    public Result<Updated> Update(
        Guid patientId,
        Guid doctorId,
        int toothNumber,
        string procedureDetails,
        decimal cost,
        Guid? appointmentId = null)
    {
        // 1. التحقق من رقم السن عبر Private Method
        if (!IsValidToothNumber(toothNumber))
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

        PatientId = patientId;
        DoctorId = doctorId;
        ToothNumber = toothNumber;
        ProcedureDetails = procedureDetails.Trim();
        AppointmentId = appointmentId;
        Cost = cost;

        return Result.Updated;
    }

    // Private Helper Method للتحقق من أرقام الأسنان المقبولة (FDI)
    private static bool IsValidToothNumber(int number)
    {
        bool isPermanent = (number >= 11 && number <= 18) ||
                            (number >= 21 && number <= 28) ||
                            (number >= 31 && number <= 38) ||
                            (number >= 41 && number <= 48);

        bool isPediatric = (number >= 51 && number <= 55) ||
                            (number >= 61 && number <= 65) ||
                            (number >= 71 && number <= 75) ||
                            (number >= 81 && number <= 85);

        return isPermanent || isPediatric;
    }
}