using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Tests.Common.Patients;

public static class PatientFactory
{
    public static Result<Patient> CreatePatient(
        string firstName = "John",
        string lastName = "Doe",
        ContactInfo? contactInfo = null,
        DateTime? dateOfBirth = null,
        Gender? gender = null)
    {
        return Patient.Create(
            firstName,
            lastName,
            contactInfo ?? ContactInfo.Create("741085207410", null, true, "koko.com").Value,
            dateOfBirth ?? DateTime.UtcNow.AddYears(-30),
            gender ?? Gender.Male);
    }
}