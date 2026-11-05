using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.TreatmentPlans;

public static class TreatmentPlanErrors
{
    public static readonly Error PatientIdRequired = Error.Validation(
        "TreatmentPlan.PatientIdRequired",
        "Patient ID is required.");

    public static readonly Error DoctorIdRequired = Error.Validation(
        "TreatmentPlan.DoctorIdRequired",
        "Doctor ID is required.");

    public static readonly Error TitleRequired = Error.Validation(
        "TreatmentPlan.TitleRequired",
        "Treatment plan title is required.");

    public static readonly Error TitleTooLong = Error.Validation(
        "TreatmentPlan.TitleTooLong",
        "Treatment plan title cannot exceed 200 characters.");

    public static readonly Error NotesTooLong = Error.Validation(
        "TreatmentPlan.NotesTooLong",
        "Treatment plan notes cannot exceed 1000 characters.");

    public static readonly Error NotFound = Error.NotFound(
        "TreatmentPlan.NotFound",
        "Treatment plan not found.");

    public static readonly Error PlanAlreadyActive = Error.Conflict(
        "TreatmentPlan.AlreadyActive",
        "The treatment plan is already active.");

    public static readonly Error PlanAlreadyCompleted = Error.Conflict(
        "TreatmentPlan.AlreadyCompleted",
        "The treatment plan is already completed.");

    public static readonly Error PlanAlreadyCancelled = Error.Conflict(
        "TreatmentPlan.AlreadyCancelled",
        "The treatment plan is already cancelled.");

    public static readonly Error PlanNotActive = Error.Conflict(
        "TreatmentPlan.NotActive",
        "This operation requires an active treatment plan. Activate it first.");

    public static readonly Error CannotModifyCancelledPlan = Error.Conflict(
        "TreatmentPlan.CannotModifyCancelledPlan",
        "A cancelled treatment plan cannot be modified.");

    public static readonly Error CannotModifyCompletedPlan = Error.Conflict(
        "TreatmentPlan.CannotModifyCompletedPlan",
        "A completed treatment plan cannot be modified.");

    // D-P5: soft delete is the only delete. Application commands must demand an
    // explicit confirmation before deleting a plan that already holds performed
    // (potentially billed) items — the domain never hard-deletes.
    public static readonly Error PlanHasPerformedItems = Error.Conflict(
        "TreatmentPlan.PlanHasPerformedItems",
        "This treatment plan has performed items. Confirm to delete it anyway.");
}