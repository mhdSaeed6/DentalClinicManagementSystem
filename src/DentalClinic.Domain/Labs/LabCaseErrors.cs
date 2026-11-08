using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Labs;

public static class LabCaseErrors
{
    public static readonly Error PatientIdRequired = Error.Validation(
        "LabCase.PatientIdRequired",
        "Patient ID is required.");

    public static readonly Error DoctorIdRequired = Error.Validation(
        "LabCase.DoctorIdRequired",
        "Doctor ID is required.");

    public static readonly Error LabNameRequired = Error.Validation(
        "LabCase.LabNameRequired",
        "The dental lab name is required.");

    public static readonly Error CaseTypeRequired = Error.Validation(
        "LabCase.CaseTypeRequired",
        "The case type is required.");

    public static readonly Error InvalidCost = Error.Validation(
        "LabCase.InvalidCost",
        "Lab cost cannot be negative.");

    public static readonly Error InvalidToothNumber = Error.Validation(
        "LabCase.InvalidToothNumber",
        "Tooth number must be a valid dental chart number (1-32 or 51-85).");

    public static readonly Error InvalidPlannedDates = Error.Validation(
        "LabCase.InvalidPlannedDates",
        "The expected return date cannot be before the sent date.");

    public static readonly Error InvalidReturnedAt = Error.Conflict(
        "LabCase.InvalidReturnedAt",
        "The returned date cannot be before the sent date.");

    public static readonly Error InvalidStatusTransition = Error.Conflict(
        "LabCase.InvalidStatusTransition",
        "The requested lab-case status change does not follow the lab workflow.");

    public static readonly Error StatusAlreadySet = Error.Conflict(
        "LabCase.StatusAlreadySet",
        "The lab case is already in the requested status.");

    // الحالة النهائية: حالة مُسلَّمة أو فاشلة لا تُعدَّل بياناتها التشغيلية.
    public static readonly Error CaseClosed = Error.Conflict(
        "LabCase.CaseClosed",
        "A returned or failed lab case cannot be edited.");
}