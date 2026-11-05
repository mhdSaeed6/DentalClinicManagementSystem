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

    // P1.1 — treatment plans (Option A): plan items are treatment records.
    public static readonly Error PlanIdRequired = Error.Validation(
        "TreatmentRecord.PlanIdRequired",
        "A planned item must reference its treatment plan.");

    public static readonly Error InvalidEstimatedCost = Error.Validation(
        "TreatmentRecord.InvalidEstimatedCost",
        "Estimated cost cannot be negative.");

    public static readonly Error InvalidSessionNumber = Error.Validation(
        "TreatmentRecord.InvalidSessionNumber",
        "Session number must be greater than zero.");

    public static readonly Error ItemAlreadyPerformed = Error.Conflict(
        "TreatmentRecord.ItemAlreadyPerformed",
        "This item has already been performed and cannot be performed again.");

    public static readonly Error ItemAlreadyCancelled = Error.Conflict(
        "TreatmentRecord.ItemAlreadyCancelled",
        "This item is already cancelled.");

    public static readonly Error ItemCancelled = Error.Conflict(
        "TreatmentRecord.ItemCancelled",
        "A cancelled item cannot be performed. Re-plan it as a new item if needed.");

    public static readonly Error ItemNotPerformed = Error.Conflict(
        "TreatmentRecord.ItemNotPerformed",
        "Only a performed item can be reopened.");

    public static readonly Error ItemNotPlanned = Error.Conflict(
        "TreatmentRecord.ItemNotPlanned",
        "Plan details (estimate and session) can only be edited while the item is planned.");
}