using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Tests.Common.Builders;

public class PatientTestDataBuilder : ITestDataBuilder<Patient>
{
    private readonly DateTime _dateOfBirth = DateTime.UtcNow.AddYears(-25);
    private readonly MedicalHistory? _medicalHistory = null;
    private string _firstName = "John";
    private string _lastName = "Doe";
    private ContactInfo _contactInfo = ContactInfo.Create("+963911111111").Value;
    private Gender _gender = Gender.Male;

    public static PatientTestDataBuilder Create() => new();

    public PatientTestDataBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public PatientTestDataBuilder WithContactInfo(ContactInfo contactInfo)
    {
        _contactInfo = contactInfo;
        return this;
    }

    public PatientTestDataBuilder WithGender(Gender gender)
    {
        _gender = gender;
        return this;
    }

    public Patient Build()
    {
        return Patient.Create(
            _firstName,
            _lastName,
            _contactInfo,
            _dateOfBirth,
            _gender,
            _medicalHistory).Value;
    }
}