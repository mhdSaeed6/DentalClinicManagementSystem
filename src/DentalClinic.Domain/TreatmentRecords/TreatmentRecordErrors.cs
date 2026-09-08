using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.TreatmentRecords;

public static class TreatmentRecordErrors
{
    public static readonly Error InvalidToothNumber = Error.Validation(
        "TreatmentRecord.InvalidToothNumber",
        "Tooth number must be a valid dental chart number (1-32 or 51-85).");

    public static readonly Error ProcedureDetailsRequired = Error.Validation(
        "TreatmentRecord.ProcedureDetailsRequired",
        "Procedure details are required.");

    public static readonly Error InvalidCost = Error.Validation(
        "TreatmentRecord.InvalidCost",
        "Cost cannot be negative.");

    public static readonly Error PatientIdRequired = Error.Validation(
        "TreatmentRecord.PatientIdRequired",
        "Patient ID is required.");

    public static readonly Error DoctorIdRequired = Error.Validation(
        "TreatmentRecord.DoctorIdRequired",
        "Doctor ID is required.");
}