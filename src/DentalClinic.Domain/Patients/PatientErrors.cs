using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Patients;

public static class PatientErrors
{
    public static Error FirstNameRequired => Error.Validation("Patient_FirstName_Required", "Patient FirstName is required.");
    public static Error LastNameRequired => Error.Validation("Patient_LastName_Required", "Patient LastName is required.");
    public static Error ContactInfoRequired => Error.Validation("Patient_ContactInfo_Required", "Patient ContactInfo is required.");
    public static Error GenderRequired => Error.Validation("Patient_Gender_Required", "Patient Gender is required.");
    public static Error InvalidDateOfBirth => Error.Validation("Patient_DateOfBirth_Invalid", "Patient DateOfBirth cannot be in the future or 150 years in the past.");
}