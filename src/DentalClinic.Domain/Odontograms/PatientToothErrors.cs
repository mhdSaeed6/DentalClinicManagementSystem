using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Odontograms;

public static class PatientToothErrors
{
    public static readonly Error PatientIdRequired = Error.Validation(
        "PatientTooth.PatientIdRequired",
        "Patient ID is required.");

    public static readonly Error InvalidToothNumber = Error.Validation(
        "PatientTooth.InvalidToothNumber",
        "Tooth number must be a valid FDI dental chart number (11-48 permanent, 51-85 pediatric).");

    public static readonly Error InvalidCondition = Error.Validation(
        "PatientTooth.InvalidCondition",
        "Tooth condition must be a defined value.");

    public static readonly Error InvalidSurface = Error.Validation(
        "PatientTooth.InvalidSurface",
        "Tooth surface must be a defined value or omitted.");

    public static readonly Error NotesTooLong = Error.Validation(
        "PatientTooth.NotesTooLong",
        "Notes must not exceed 500 characters.");
}