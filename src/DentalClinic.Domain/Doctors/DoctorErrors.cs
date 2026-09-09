using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Doctors;

public static class DoctorErrors
{
    public static readonly Error FirstNameRequired = Error.Validation(
        "Doctor.FirstNameRequired", "First name is required.");
    public static readonly Error ContactInfoRequired = Error.Validation(
        "Doctor.ContactInfoRequired", "Contact info is required.");

    public static readonly Error LastNameRequired = Error.Validation(
        "Doctor.LastNameRequired", "Last name is required.");

    public static readonly Error SpecializationRequired = Error.Validation(
        "Doctor.SpecializationRequired", "Specialization is required.");

    public static readonly Error GenderRequired = Error.Validation(
        "Doctor.GenderRequired", "Valid gender is required.");

    public static readonly Error DoctorInactive = Error.Validation(
        "Doctor.Inactive", "Cannot perform operations on an inactive doctor.");
}